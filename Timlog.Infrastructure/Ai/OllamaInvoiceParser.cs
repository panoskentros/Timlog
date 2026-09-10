using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Timlog.Application.Interfaces;
using Timlog.Application.Models;

namespace Timlog.Infrastructure.Ai;

public class OllamaInvoiceParser : IInvoiceTextParser
{
    private readonly HttpClient _httpClient;
    private readonly string _modelName;
    private readonly ILogger<OllamaInvoiceParser> _logger;

    public OllamaInvoiceParser(
        HttpClient httpClient, 
        IConfiguration configuration,
        ILogger<OllamaInvoiceParser> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _modelName = configuration["Ollama:Model"] ?? throw new InvalidOperationException("Ollama:Model is missing.");
        
        var baseUrl = configuration["Ollama:BaseUrl"] ?? throw new InvalidOperationException("Ollama:BaseUrl is missing.");
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<Result<ParsedInvoiceDto>> ParseAsync(string text, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Αποστολή κειμένου στο μοντέλο {ModelName} του Ollama (Μήκος κειμένου: {Length} χαρακτήρες)", _modelName, text.Length);

        var stopwatch = Stopwatch.StartNew();

        var payload = new
        {
            model = _modelName,
            prompt = text,
            system = "You are a strict B2B invoice data extractor. Return ONLY valid JSON: {\"ClientName\": \"string\", \"ClientAfm\": \"string\", \"NetAmount\": number, \"WithholdingTax\": boolean, \"Description\": \"string\", \"InvoiceType\": number}. " +
                     "STRICT RULES: " +
                     "1. ClientAfm MUST be extracted EXACTLY as it appears in the CURRENT input text. Preserve leading zeros and country prefixes. DO NOT invent, guess, or reuse VAT numbers from previous texts. If no VAT is explicitly written, return an empty string (\"\"). " +
                     "2. NetAmount must be a raw numeric value without quotes (e.g., 1000.00, never \"1000.00\"). Convert commas to dots. " +
                     "3. WithholdingTax is true ONLY if 'παρακράτηση' or 'withholding' is explicitly mentioned. " +
                     "4. InvoiceType mapping rules: " +
                     "- Type 1: For any 9-digit numeric AFM without a prefix (e.g., '094259216', '179741085') or any AFM prefixed with EL/GR (e.g., 'EL094259216'). " +
                     "- Type 2: For EU VAT IDs starting with country letters other than Greece (e.g., DE, FR, CY, IT). " +
                     "- Type 3: For Non-EU VAT IDs (e.g., US, GB, CH). " +
                     "5. Output absolutely nothing else besides the JSON object.",
            stream = false,
            format = "json"
        };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync("/api/generate", payload, cancellationToken);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Σφάλμα δικτύου κατά την επικοινωνία με το Ollama μετά από {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
            return Result<ParsedInvoiceDto>.Fail("Αποτυχία σύνδεσης με την υπηρεσία AI.");
        }

        stopwatch.Stop();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Η κλήση στο Ollama απέτυχε με status {StatusCode} μετά από {ElapsedMilliseconds}ms", response.StatusCode, stopwatch.ElapsedMilliseconds);
            return Result<ParsedInvoiceDto>.Fail($"Αποτυχία επικοινωνίας με το Ollama (Κωδικός: {response.StatusCode}).");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(jsonResponse);
        
        var extractedText = document.RootElement.GetProperty("response").GetString();

        if (string.IsNullOrWhiteSpace(extractedText))
        {
            _logger.LogWarning("Το Ollama επέστρεψε κενή απάντηση (Response field is empty) σε {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
            return Result<ParsedInvoiceDto>.Fail("Το Ollama επέστρεψε κενή απάντηση.");
        }

        ParsedInvoiceDto? parsedResult;
        try
        {
            var options = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString 
            };
    
            parsedResult = JsonSerializer.Deserialize<ParsedInvoiceDto>(extractedText, options);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Αποτυχία deserialize του JSON από το Ollama. Raw AI output: {RawOutput}", extractedText);
            return Result<ParsedInvoiceDto>.Fail("Αποτυχία αποσειριοποίησης των δεδομένων του τιμολογίου.");
        }

        if (parsedResult == null)
        {
            _logger.LogWarning("Το αποτέλεσμα του deserialize ήταν null. Raw AI output: {RawOutput}", extractedText);
            return Result<ParsedInvoiceDto>.Fail("Αποτυχία αποσειριοποίησης των δεδομένων του τιμολογίου.");
        }

        _logger.LogInformation("Επιτυχής ανάλυση κειμένου από το Ollama σε {ElapsedMilliseconds}ms (Εξαχθέν ΑΦΜ: {ClientAfm}, Ποσό: {NetAmount})", 
            stopwatch.ElapsedMilliseconds, 
            parsedResult.ClientAfm, 
            parsedResult.NetAmount);

        return Result<ParsedInvoiceDto>.Ok(parsedResult);
    }
}
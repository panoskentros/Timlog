using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Timlog.Application.Interfaces;
using Timlog.Application.Models;

namespace Timlog.Infrastructure.Ai;

public class GeminiInvoiceParser : IInvoiceTextParser
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _modelName;
    private readonly ILogger<GeminiInvoiceParser> _logger;

    public GeminiInvoiceParser(
        HttpClient httpClient, 
        IConfiguration configuration,
        ILogger<GeminiInvoiceParser> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["Gemini:ApiKey"] ?? throw new InvalidOperationException("Gemini API Key is missing.");
        _modelName = configuration["Gemini:Model"] ?? "gemini-2.5-flash";
    }

    public async Task<Result<ParsedInvoiceDto>> ParseAsync(string text, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Αποστολή κειμένου στο Gemini API (Μοντέλο: {ModelName}, Μήκος: {Length} χαρακτήρες)", _modelName, text.Length);

        var stopwatch = Stopwatch.StartNew();
        var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelName}:generateContent";

        var payload = new
        {
            contents = new[]
            {
                new 
                {
                    parts = new[] { new { text = text } }
                }
            },
            system_instruction = new
            {
                parts = new[] 
                { 
                    new 
                    { 
                        text = "You are a strict B2B invoice data extractor. Return ONLY valid JSON: {\"ClientName\": \"string\", \"ClientAfm\": \"string\", \"NetAmount\": number, \"WithholdingTax\": boolean, \"Description\": \"string\", \"InvoiceType\": number}. " +
                               "STRICT RULES: " +
                               "1. ClientAfm MUST be extracted EXACTLY as it appears in the CURRENT input text. Preserve leading zeros and country prefixes. DO NOT invent, guess, or reuse VAT numbers from previous texts. If no VAT is explicitly written, return an empty string (\"\"). " +
                               "2. NetAmount must be a raw numeric value without quotes (e.g., 1000.00, never \"1000.00\"). Convert commas to dots. " +
                               "3. WithholdingTax is true ONLY if 'παρακράτηση' or 'withholding' is explicitly mentioned. " +
                               "4. InvoiceType mapping rules: " +
                               "- Type 1: For any 9-digit numeric AFM without a prefix (e.g., '094259216', '179741085') or any AFM prefixed with EL/GR (e.g., 'EL094259216'). " +
                               "- Type 2: For EU VAT IDs starting with country letters other than Greece (e.g., DE, FR, CY, IT). " +
                               "- Type 3: For Non-EU VAT IDs (e.g., US, GB, CH). " +
                               "5. Output absolutely nothing else besides the JSON object."
                    } 
                }
            },
            generationConfig = new
            {
                response_mime_type = "application/json"
            }
        };

        HttpResponseMessage response;
        try
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = JsonContent.Create(payload)
            };
            
            requestMessage.Headers.Add("X-goog-api-key", _apiKey);

            response = await _httpClient.SendAsync(requestMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Σφάλμα δικτύου κατά την επικοινωνία με το Gemini API μετά από {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
            return Result<ParsedInvoiceDto>.Fail("Αποτυχία σύνδεσης με την υπηρεσία AI.");
        }

        stopwatch.Stop();

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Η κλήση στο Gemini απέτυχε με status {StatusCode} σε {ElapsedMilliseconds}ms. Response: {ErrorBody}", response.StatusCode, stopwatch.ElapsedMilliseconds, errorBody);
            return Result<ParsedInvoiceDto>.Fail($"Αποτυχία επικοινωνίας με το Gemini API (Κωδικός: {response.StatusCode}).");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        
        using var document = JsonDocument.Parse(jsonResponse);
        
        if (!document.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
        {
            _logger.LogWarning("Το Gemini δεν επέστρεψε υποψήφιες απαντήσεις (candidates array empty) σε {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
            return Result<ParsedInvoiceDto>.Fail("Το Gemini επέστρεψε κενή απάντηση.");
        }

        var extractedText = candidates[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        if (string.IsNullOrWhiteSpace(extractedText))
        {
            _logger.LogWarning("Το πεδίο κειμένου της απάντησης του Gemini είναι κενό");
            return Result<ParsedInvoiceDto>.Fail("Το Gemini επέστρεψε κενή απάντηση.");
        }

        ParsedInvoiceDto? parsedResult;
        try
        {
            parsedResult = JsonSerializer.Deserialize<ParsedInvoiceDto>(extractedText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Αποτυχία deserialize του JSON από το Gemini. Raw response: {RawOutput}", extractedText);
            return Result<ParsedInvoiceDto>.Fail("Αποτυχία αποσειριοποίησης των δεδομένων του τιμολογίου.");
        }

        if (parsedResult == null)
        {
            _logger.LogWarning("Το αποτέλεσμα του deserialize ήταν null. Raw response: {RawOutput}", extractedText);
            return Result<ParsedInvoiceDto>.Fail("Αποτυχία αποσειριοποίησης των δεδομένων του τιμολογίου.");
        }

        _logger.LogInformation("Επιτυχής ανάλυση κειμένου από το Gemini σε {ElapsedMilliseconds}ms (Εξαχθέν ΑΦΜ: {ClientAfm}, Ποσό: {NetAmount})", 
            stopwatch.ElapsedMilliseconds, 
            parsedResult.ClientAfm, 
            parsedResult.NetAmount);

        return Result<ParsedInvoiceDto>.Ok(parsedResult);
    }
}
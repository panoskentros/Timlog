using System.ClientModel;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using Timlog.Application.Interfaces;
using Timlog.Application.Models;

namespace Timlog.Infrastructure.Ai;

public class OpenAiInvoiceParser : IInvoiceTextParser
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<OpenAiInvoiceParser> _logger;
    private const string ModelName = "gpt-5.6-luna";

    public OpenAiInvoiceParser(string apiKey, ILogger<OpenAiInvoiceParser> logger)
    {
        _chatClient = new ChatClient(ModelName, apiKey);
        _logger = logger;
    }

    public async Task<Result<ParsedInvoiceDto>> ParseAsync(string text, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Αποστολή κειμένου στο μοντέλο {ModelName} του OpenAI (Μήκος: {Length} χαρακτήρες)", ModelName, text.Length);

        var stopwatch = Stopwatch.StartNew();

        var systemPrompt = @"You are a strict B2B invoice data extractor. Return ONLY valid JSON: {""ClientName"": ""string"", ""ClientAfm"": ""string"", ""NetAmount"": number, ""WithholdingTax"": boolean, ""Description"": ""string"", ""InvoiceType"": number}. 
            STRICT RULES: 
            1. ClientAfm MUST be extracted EXACTLY as it appears in the CURRENT input text. Preserve leading zeros and country prefixes. DO NOT invent, guess, or reuse VAT numbers from previous texts. If no VAT is explicitly written, return an empty string (""""). 
            2. NetAmount must be a raw numeric value without quotes (e.g., 1000.00, never ""1000.00""). Convert commas to dots. 
            3. WithholdingTax is true ONLY if 'παρακράτηση' or 'withholding' is explicitly mentioned. 
            4. InvoiceType mapping rules: 
            - Type 1: For any 9-digit numeric AFM without a prefix (e.g., '094259216', '179741085') or any AFM prefixed with EL/GR (e.g., 'EL094259216'). 
            - Type 2: For EU VAT IDs starting with country letters other than Greece (e.g., DE, FR, CY, IT). 
            - Type 3: For Non-EU VAT IDs (e.g., US, GB, CH). 
            5. Output absolutely nothing else besides the JSON object.";

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(text)
        };

        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
        };

        ClientResult<ChatCompletion> response;
        try
        {
            response = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Σφάλμα κατά την επικοινωνία με το OpenAI API μετά από {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
            return Result<ParsedInvoiceDto>.Fail("Αποτυχία σύνδεσης με την υπηρεσία AI.");
        }

        stopwatch.Stop();

        var jsonResponse = response.Value?.Content?[0]?.Text;

        if (string.IsNullOrWhiteSpace(jsonResponse))
        {
            _logger.LogWarning("Το OpenAI επέστρεψε κενή απάντηση σε {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
            return Result<ParsedInvoiceDto>.Fail("Το OpenAI επέστρεψε κενή απάντηση.");
        }

        ParsedInvoiceDto? parsedResult;
        try
        {
            parsedResult = JsonSerializer.Deserialize<ParsedInvoiceDto>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Αποτυχία deserialize του JSON από το OpenAI. Raw AI output: {RawOutput}", jsonResponse);
            return Result<ParsedInvoiceDto>.Fail("Αποτυχία αποσειριοποίησης των δεδομένων του τιμολογίου.");
        }

        if (parsedResult == null)
        {
            _logger.LogWarning("Το αποτέλεσμα του deserialize ήταν null. Raw AI output: {RawOutput}", jsonResponse);
            return Result<ParsedInvoiceDto>.Fail("Αποτυχία αποσειριοποίησης των δεδομένων του τιμολογίου.");
        }

        _logger.LogInformation("Επιτυχής ανάλυση κειμένου από το OpenAI σε {ElapsedMilliseconds}ms (Εξαχθέν ΑΦΜ: {ClientAfm}, Ποσό: {NetAmount})",
            stopwatch.ElapsedMilliseconds,
            parsedResult.ClientAfm,
            parsedResult.NetAmount);

        return Result<ParsedInvoiceDto>.Ok(parsedResult);
    }
}
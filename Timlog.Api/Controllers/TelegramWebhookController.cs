using System.IO;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QRCoder;
using Timlog.Application.Commands;
using Timlog.Application.Interfaces;
using Timlog.Application.Models;
using Timlog.Infrastructure.Telegram;

namespace Timlog.Api.Controllers;

[ApiController]
[Route("api/telegram")]
public class TelegramWebhookController : ControllerBase
{
    private readonly CreateInvoiceFromTextCommandHandler _invoiceHandler;
    private readonly SetupFlowHandler _setupFlowHandler;
    private readonly ApproveInvoiceCommandHandler _approveHandler;
    private readonly ITelegramClient _telegramClient;
    private readonly ILogger<TelegramWebhookController> _logger;

    public TelegramWebhookController(
        CreateInvoiceFromTextCommandHandler invoiceHandler,
        SetupFlowHandler setupFlowHandler,
        ApproveInvoiceCommandHandler approveHandler,
        ITelegramClient telegramClient,
        ILogger<TelegramWebhookController> logger)
    {
        _invoiceHandler = invoiceHandler;
        _setupFlowHandler = setupFlowHandler;
        _approveHandler = approveHandler;
        _telegramClient = telegramClient;
        _logger = logger;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> HandleWebhook([FromBody] TelegramUpdateDto update, CancellationToken cancellationToken)
    {
        if (update.CallbackQuery != null)
        {
            var chatId = update.CallbackQuery.Message.Chat.Id;
            var messageId = update.CallbackQuery.Message.MessageId;
            var data = update.CallbackQuery.Data;

            if (data != null && data.StartsWith("approve_"))
            {
                var invoiceId = Guid.Parse(data.Replace("approve_", ""));
                _logger.LogInformation("Λήφθηκε callback έγκρισης για τιμολόγιο {InvoiceId} από ChatId: {ChatId}", invoiceId, chatId);

                await _telegramClient.EditMessageTextAsync(chatId, (int)messageId, "⌛ Το τιμολόγιο εγκρίνεται, παρακαλώ περιμένετε...", cancellationToken);
                var command = new ApproveInvoiceCommand(invoiceId, chatId);
                var result = await _approveHandler.HandleAsync(command, cancellationToken);

                if (result.Success)
                {
                    await _telegramClient.EditMessageTextAsync(chatId, (int)messageId, "✅ Το τιμολόγιο εγκρίθηκε και εκδόθηκε επιτυχώς!", cancellationToken);

                    if (!string.IsNullOrEmpty(result.Data.QrUrl))
                    {
                        string caption = $"MARK: {result.Data.Mark}\nID: {invoiceId}\n\nΕπίσημη Επισκόπηση:\n{result.Data.QrUrl}";

                        var qrGenerator = new QRCodeGenerator();
                        var qrCodeData = qrGenerator.CreateQrCode(result.Data.QrUrl, QRCodeGenerator.ECCLevel.Q);
                        var qrCode = new PngByteQRCode(qrCodeData);
                        byte[] qrBytes = qrCode.GetGraphic(20);

                        using var stream = new MemoryStream(qrBytes);

                        await _telegramClient.SendPhotoAsync(
                            chatId,
                            stream,
                            "qrcode.png",
                            caption,
                            cancellationToken);
                    }
                }
                else
                {
                    _logger.LogWarning("Αποτυχία έγκρισης τιμολογίου {InvoiceId} για ChatId: {ChatId}. Σφάλμα: {Error}", invoiceId, chatId, result.Message);
                    await _telegramClient.EditMessageTextAsync(chatId, (int)messageId, $"❌ Σφάλμα έκδοσης: {result.Message}", cancellationToken);
                }
            }
            else if (data != null && data.StartsWith("cancel_"))
            {
                var invoiceId = data.Replace("cancel_", "");
                _logger.LogInformation("Ακύρωση έκδοσης προσχεδίου {InvoiceId} από ChatId: {ChatId}", invoiceId, chatId);
                await _telegramClient.EditMessageTextAsync(chatId, (int)messageId, "🚫 Η έκδοση του τιμολογίου ακυρώθηκε.", cancellationToken);
            }

            return Ok();
        }

        if (update.Message?.Text != null && update.Message.From != null)
        {
            var chatId = update.Message.Chat.Id;
            var text = update.Message.Text.Trim();

            try
            {
                if (text.Equals("/setup", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Έναρξη /setup flow για ChatId: {ChatId}", chatId);
                    _setupFlowHandler.Start(chatId);
                    await _telegramClient.SendTextMessageAsync(chatId, "Έναρξη ρύθμισης. Εισάγετε το ΑΦΜ σας (πληκτρολογήστε /cancel για ακύρωση):", cancellationToken);
                    return Ok();
                }

                if (_setupFlowHandler.IsInSetup(chatId))
                {
                    var reply = await _setupFlowHandler.ProcessStepAsync(chatId, text, cancellationToken);
                    await _telegramClient.SendTextMessageAsync(chatId, reply, cancellationToken);
                    return Ok();
                }

                _logger.LogInformation("Επεξεργασία μηνύματος κειμένου για έκδοση τιμολογίου από ChatId: {ChatId}", chatId);
                var invoiceCommand = new CreateInvoiceFromTextCommand(text);
                var result = await _invoiceHandler.HandleAsync(chatId, invoiceCommand, cancellationToken);

                if (!result.Success)
                {
                    _logger.LogWarning("Αποτυχία επεξεργασίας κειμένου τιμολογίου για ChatId: {ChatId}. Σφάλμα: {Error}", chatId, result.Message);
                    await _telegramClient.SendTextMessageAsync(chatId, $"Σφάλμα: {result.Message}", cancellationToken);
                    return Ok();
                }

                var sb = new StringBuilder();

                sb.AppendLine("Προεπισκόπηση Προσχεδίου Τιμολογίου:\n");
                sb.AppendLine($"Τύπος: {result.Data.InvoiceType}");
                sb.AppendLine($"Αριθμός: {result.Data.InvoiceNumber}");
                sb.AppendLine($"ΑΦΜ Πελάτη: {result.Data.ClientAfm}");
                sb.AppendLine($"Καθαρή Αξία: {result.Data.NetAmount:C}");
                sb.AppendLine($"ΦΠΑ: {result.Data.VatAmount:C}");

                if (result.Data.WithholdingAmount > 0)
                {
                    sb.AppendLine($"Παρακράτηση Φόρου (20%): -{result.Data.WithholdingAmount:C}");
                }

                sb.AppendLine($"Συνολικό Πληρωτέο: {result.Data.TotalPayable:C}");

                if (result.Data.WithholdingTaxRequested && result.Data.NetAmount <= 300m)
                {
                    sb.AppendLine("\n⚠️ Σημείωση: Δεν εφαρμόστηκε παρακράτηση φόρου διότι η καθαρή αξία δεν υπερβαίνει τα 300,00 € (άρθρο 64 Ν.4172/2013).");
                }

                sb.AppendLine("\nΕπιθυμείς την έκδοση;");

                var previewText = sb.ToString();

                var buttons = new[]
                {
                    new[]
                    {
                        ("Έγκριση & Έκδοση", $"approve_{result.Data.InvoiceId}"),
                        ("Ακύρωση", $"cancel_{result.Data.InvoiceId}")
                    }
                };

                await _telegramClient.SendTextMessageWithInlineKeyboardAsync(chatId, previewText, buttons, cancellationToken);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Σφάλμα εγκυρότητας ορίσματος για ChatId: {ChatId}", chatId);
                await _telegramClient.SendTextMessageAsync(chatId, 
                    $"Σφάλμα εγκυρότητας: {ex.Message}", 
                    cancellationToken);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Απρόσμενο σφάλμα κατά την ανάλυση κειμένου για ChatId: {ChatId}", chatId);
                await _telegramClient.SendTextMessageAsync(
                    chatId, 
                    $"Προέκυψε ένα απρόσμενο σφάλμα κατά την ανάλυση του κειμένου. Δοκίμασε ξανά. {ex.Message}", 
                    cancellationToken);
            }
        }

        return Ok();
    }
}
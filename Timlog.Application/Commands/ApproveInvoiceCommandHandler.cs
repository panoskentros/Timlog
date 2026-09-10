using Microsoft.Extensions.Logging;
using Timlog.Application.Interfaces;
using Timlog.Application.Models;

namespace Timlog.Application.Commands;

public record ApproveInvoiceCommand(Guid InvoiceId, long TelegramChatId);

public record ApproveInvoiceResult(string? Mark, string? QrUrl);

public class ApproveInvoiceCommandHandler
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ICredentialRepository _credentialRepository;
    private readonly ICredentialEncryptionService _encryptionService;
    private readonly IAadeClient _aadeClient;
    private readonly ILogger<ApproveInvoiceCommandHandler> _logger;

    public ApproveInvoiceCommandHandler(
        IInvoiceRepository repository,
        ICredentialRepository credentialRepository,
        ICredentialEncryptionService encryptionService,
        IAadeClient aadeClient,
        ILogger<ApproveInvoiceCommandHandler> logger)
    {
        _invoiceRepository = repository;
        _credentialRepository = credentialRepository;
        _encryptionService = encryptionService;
        _aadeClient = aadeClient;
        _logger = logger;
    }

    public async Task<Result<ApproveInvoiceResult>> HandleAsync(ApproveInvoiceCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Έναρξη διαδικασίας έγκρισης και έκδοσης τιμολογίου {InvoiceId} για ChatId: {ChatId}", command.InvoiceId, command.TelegramChatId);

        var invoice = await _invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (invoice == null || !invoice.IsDraft)
        {
            _logger.LogWarning("Το προσχέδιο τιμολογίου {InvoiceId} δεν βρέθηκε ή έχει ήδη εκδοθεί.", command.InvoiceId);
            return Result<ApproveInvoiceResult>.Fail("Invoice draft not found or already issued.");
        }

        var credential = await _credentialRepository.GetByChatIdAsync(command.TelegramChatId, cancellationToken);
        if (credential == null)
        {
            _logger.LogWarning("Δεν βρέθηκαν διαπιστευτήρια για ChatId: {ChatId} κατά την έγκριση του τιμολογίου {InvoiceId}", command.TelegramChatId, command.InvoiceId);
            return Result<ApproveInvoiceResult>.Fail("Credentials not found.");
        }

        var decryptedUserId = _encryptionService.Decrypt(credential.EncryptedAadeUserId);
        var decryptedSubscriptionKey = _encryptionService.Decrypt(credential.EncryptedSubscriptionKey);

        _logger.LogInformation("Αποστολή τιμολογίου {InvoiceId} στην ΑΑΔΕ (myDATA)...", command.InvoiceId);
        var aadeResult = await _aadeClient.SendInvoiceAsync(invoice, credential, decryptedUserId, decryptedSubscriptionKey, cancellationToken);
        if (!aadeResult.Success)
        {
            _logger.LogError("Η αποστολή στην ΑΑΔΕ απέτυχε για το τιμολόγιο {InvoiceId}. Σφάλμα: {Error}", command.InvoiceId, aadeResult.Message);
            return Result<ApproveInvoiceResult>.Fail(aadeResult.Message ?? "AADE error.");
        }

        invoice.MarkAsIssued(aadeResult.Data.Mark, aadeResult.Data.QrUrl);
        await _invoiceRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Το τιμολόγιο {InvoiceId} εκδόθηκε επιτυχώς με MARK: {Mark}", command.InvoiceId, aadeResult.Data.Mark);

        var resultData = new ApproveInvoiceResult(aadeResult.Data.Mark, aadeResult.Data.QrUrl);
        return Result<ApproveInvoiceResult>.Ok(resultData);
    }
}
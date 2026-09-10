using FluentValidation;
using Microsoft.Extensions.Logging;
using Timlog.Application.Interfaces;
using Timlog.Application.Models;
using Timlog.Domain.Entities;

namespace Timlog.Application.Commands;

public class SetupCredentialsCommandHandler
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly ICredentialEncryptionService _encryptionService;
    private readonly IValidator<SetupCredentialsCommand> _validator;
    private readonly ILogger<SetupCredentialsCommandHandler> _logger;

    public SetupCredentialsCommandHandler(
        ICredentialRepository credentialRepository,
        ICredentialEncryptionService encryptionService,
        IValidator<SetupCredentialsCommand> validator,
        ILogger<SetupCredentialsCommandHandler> logger)
    {
        _credentialRepository = credentialRepository;
        _encryptionService = encryptionService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(SetupCredentialsCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Έναρξη αποθήκευσης διαπιστευτηρίων για ChatId: {ChatId}", command.TelegramChatId);

        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Αποτυχία validation διαπιστευτηρίων για ChatId: {ChatId}. Σφάλμα: {Error}", command.TelegramChatId, validationResult.Errors[0].ErrorMessage);
            return Result.Fail(validationResult.Errors[0].ErrorMessage);
        }

        var encryptedUserId = _encryptionService.Encrypt(command.AadeUserId);
        var encryptedSubKey = _encryptionService.Encrypt(command.SubscriptionKey);

        var existing = await _credentialRepository.GetByChatIdAsync(command.TelegramChatId, cancellationToken);

        if (existing == null)
        {
            _logger.LogInformation("Δημιουργία νέου TenantCredential για ChatId: {ChatId}", command.TelegramChatId);
            var credential = new TenantCredential(
                Guid.NewGuid(),
                command.TelegramChatId,
                command.IssuerAfm,
                encryptedUserId,
                encryptedSubKey);

            credential.UpdateCompanyDetails(
                command.CompanyName,
                command.Street,
                command.Number,
                command.PostalCode,
                command.City);

            await _credentialRepository.AddAsync(credential, cancellationToken);
        }
        else
        {
            _logger.LogInformation("Ενημέρωση υπαρχόντων διαπιστευτηρίων για ChatId: {ChatId}", command.TelegramChatId);
            existing.UpdateCredentials(command.IssuerAfm, encryptedUserId, encryptedSubKey);
            existing.UpdateCompanyDetails(
                command.CompanyName,
                command.Street,
                command.Number,
                command.PostalCode,
                command.City);
        }

        await _credentialRepository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Τα διαπιστευτήρια αποθηκεύτηκαν επιτυχώς για ChatId: {ChatId}", command.TelegramChatId);

        return Result.Ok();
    }
}
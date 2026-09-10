using FluentValidation;
using Microsoft.Extensions.Logging;
using Timlog.Application.Interfaces;
using Timlog.Application.Models;
using Timlog.Domain.Entities;
using Timlog.Domain.Enums;
using Timlog.Domain.Services;
using Timlog.Domain.ValueObjects;

namespace Timlog.Application.Commands;

public class CreateInvoiceFromTextCommandHandler
{
    private readonly IInvoiceTextParser _parser;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ICredentialRepository _credentialRepository;
    private readonly IValidator<CreateInvoiceFromTextCommand> _validator;
    private readonly ILogger<CreateInvoiceFromTextCommandHandler> _logger;

    public CreateInvoiceFromTextCommandHandler(
        IInvoiceTextParser parser,
        IInvoiceRepository repository,
        ICredentialRepository credentialRepository,
        IValidator<CreateInvoiceFromTextCommand> validator,
        ILogger<CreateInvoiceFromTextCommandHandler> logger)
    {
        _logger = logger;
        _parser = parser;
        _invoiceRepository = repository;
        _credentialRepository = credentialRepository;
        _validator = validator;
    }

    public async Task<Result<CreateInvoiceResult>> HandleAsync(long chatId, CreateInvoiceFromTextCommand command, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Έναρξη επεξεργασίας αιτήματος δημιουργίας τιμολογίου για ChatId: {ChatId}", chatId);

        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Αποτυχία επικύρωσης εντολής για ChatId: {ChatId}. Σφάλμα: {Error}", chatId, validationResult.Errors[0].ErrorMessage);
            return Result<CreateInvoiceResult>.Fail(validationResult.Errors[0].ErrorMessage);
        }

        var credential = await _credentialRepository.GetByChatIdAsync(chatId, cancellationToken);
        if (credential == null)
        {
            _logger.LogWarning("Δεν βρέθηκαν διαπιστευτήρια για ChatId: {ChatId}", chatId);
            return Result<CreateInvoiceResult>.Fail("Παρακαλώ ρυθμίστε πρώτα τα διαπιστευτήριά σας στην ΑΑΔΕ χρησιμοποιώντας την εντολή /setup.");
        }

        _logger.LogInformation("Αποστολή κειμένου στο AI parser για ανάλυση. ChatId: {ChatId}", chatId);
        var parseResult = await _parser.ParseAsync(command.Text, cancellationToken);
        if (!parseResult.Success || parseResult.Data == null)
        {
            _logger.LogWarning("Αποτυχία ανάλυσης κειμένου από το AI για ChatId: {ChatId}. Σφάλμα: {Error}", chatId, parseResult.Message);
            return Result<CreateInvoiceResult>.Fail(parseResult.Message ?? "Αποτυχία ανάλυσης του κειμένου από το AI.");
        }

        var parsedDto = parseResult.Data;
        var parsedVat = parsedDto.ClientAfm?.Trim().ToUpperInvariant() ?? string.Empty;

        InvoiceType invoiceType;
        if ((parsedVat.Length == 9 && parsedVat.All(char.IsDigit)) || parsedVat.StartsWith("EL") || parsedVat.StartsWith("GR")) // σε περιπτωση που το AI γυρισει βλακειες
        {
            invoiceType = InvoiceType.ServicesDomestic;
        }
        else if (Enum.IsDefined(typeof(InvoiceType), parsedDto.InvoiceType))
        {
            invoiceType = (InvoiceType)parsedDto.InvoiceType;
        }
        else
        {
            invoiceType = InvoiceType.ServicesDomestic;
        }

        _logger.LogInformation("Υπολογισμός φορολογικών στοιχείων για ClientAfm: {ClientAfm}, NetAmount: {NetAmount}, Type: {InvoiceType}", parsedDto.ClientAfm, parsedDto.NetAmount, invoiceType);
        var taxResult = GreekTaxEngine.Calculate(parsedDto.NetAmount, invoiceType);
              
        var nextNumber = await _invoiceRepository.GetNextInvoiceNumberAsync(cancellationToken);
        var invoice = new Invoice(
            Guid.NewGuid(),
            nextNumber,
            Afm.Create(credential.IssuerAfm),
            Afm.Create(parsedDto.ClientAfm),
            parsedDto.ClientName,
            taxResult.NetAmount,
            taxResult.VatAmount,
            taxResult.WithholdingTaxAmount,
            taxResult.TotalPayableByClient,
            invoiceType);

        await _invoiceRepository.AddAsync(invoice, cancellationToken);
        await _invoiceRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Το προσχέδιο τιμολογίου {InvoiceId} δημιουργήθηκε επιτυχώς με αριθμό {InvoiceNumber} για ChatId: {ChatId}", invoice.Id, invoice.InvoiceNumber, chatId);

        var resultData = new CreateInvoiceResult(
            true, 
            null, 
            invoice.Id, 
            null, 
            null, 
            (int)invoice.InvoiceNumber,
            invoice.ClientAfm.Value, 
            invoice.NetAmount, 
            invoice.VatAmount, 
            WithholdingAmount: invoice.WithholdingAmount,
            invoice.TotalPayable,
            invoiceType.GetDisplayName(),
            WithholdingTaxRequested: parsedDto.WithholdingTax);

        return Result<CreateInvoiceResult>.Ok(resultData);
    }
}
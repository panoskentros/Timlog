using FluentValidation;
using Timlog.Application.Interfaces;
using Timlog.Application.Models;

namespace Timlog.Application.Commands;

public class SetupFlowHandler
{
    private readonly ISetupStateService _stateService;
    private readonly SetupCredentialsCommandHandler _commandHandler;
    private readonly IValidator<SetupStepInput> _stepValidator;

    public SetupFlowHandler(
        ISetupStateService stateService, 
        SetupCredentialsCommandHandler commandHandler,
        IValidator<SetupStepInput> stepValidator)
    {
        _stateService = stateService;
        _commandHandler = commandHandler;
        _stepValidator = stepValidator;
    }

    public bool IsInSetup(long chatId) => _stateService.GetSession(chatId) != null;

    public void Start(long chatId) => _stateService.StartSession(chatId);

    public async Task<string> ProcessStepAsync(long chatId, string text, CancellationToken cancellationToken = default)
    {
        var session = _stateService.GetSession(chatId);
        if (session == null)
        {
            return string.Empty;
        }

        var trimmedText = text.Trim();

        if (trimmedText.Equals("/cancel", StringComparison.OrdinalIgnoreCase))
        {
            _stateService.ClearSession(chatId);
            return "Η διαδικασία ρύθμισης ακυρώθηκε.";
        }

        var validationResult = await _stepValidator.ValidateAsync(
            new SetupStepInput(session.CurrentStep, trimmedText), 
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return $"{validationResult.Errors[0].ErrorMessage} Παρακαλώ προσπαθήστε ξανά (ή /cancel):";
        }

        switch (session.CurrentStep)
        {
            case SetupStep.AwaitingAfm:
                session.IssuerAfm = trimmedText;
                session.CurrentStep = SetupStep.AwaitingAadeUserId;
                return "1/8: Εισάγετε το AADE User ID σας:";

            case SetupStep.AwaitingAadeUserId:
                session.AadeUserId = trimmedText;
                session.CurrentStep = SetupStep.AwaitingSubscriptionKey;
                return "2/8: Εισάγετε το AADE Subscription Key:";

            case SetupStep.AwaitingSubscriptionKey:
                session.SubscriptionKey = trimmedText;
                session.CurrentStep = SetupStep.AwaitingCompanyName;
                return "3/8: Εισάγετε την Επωνυμία της επιχείρησής σας:";

            case SetupStep.AwaitingCompanyName:
                session.CompanyName = trimmedText;
                session.CurrentStep = SetupStep.AwaitingStreet;
                return "4/8: Εισάγετε την Οδό της έδρας:";

            case SetupStep.AwaitingStreet:
                session.Street = trimmedText;
                session.CurrentStep = SetupStep.AwaitingNumber;
                return "5/8: Εισάγετε τον Αριθμό της οδού:";

            case SetupStep.AwaitingNumber:
                session.Number = trimmedText;
                session.CurrentStep = SetupStep.AwaitingPostalCode;
                return "6/8: Εισάγετε τον Ταχυδρομικό Κώδικα (Τ.Κ.):";

            case SetupStep.AwaitingPostalCode:
                session.PostalCode = trimmedText;
                session.CurrentStep = SetupStep.AwaitingCity;
                return "7/8: Εισάγετε την Πόλη:";

            case SetupStep.AwaitingCity:
                session.City = trimmedText;

                var command = new SetupCredentialsCommand(
                    chatId,
                    session.IssuerAfm!,
                    session.AadeUserId!,
                    session.SubscriptionKey!,
                    session.CompanyName,
                    session.Street,
                    session.Number,
                    session.PostalCode,
                    session.City);

                var result = await _commandHandler.HandleAsync(command, cancellationToken);
                _stateService.ClearSession(chatId);

                if (!result.Success)
                {
                    return $"Σφάλμα κατά την αποθήκευση: {result.Message}";
                }

                return "Η ρύθμιση ολοκληρώθηκε επιτυχώς! Τα στοιχεία αποθηκεύτηκαν.";

            default:
                _stateService.ClearSession(chatId);
                return "Σφάλμα ροής. Ξεκινήστε ξανά πληκτρολογώντας /setup.";
        }
    }
}
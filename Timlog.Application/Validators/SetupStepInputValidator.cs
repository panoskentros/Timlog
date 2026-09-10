using FluentValidation;
using Timlog.Application.Commands;
using Timlog.Application.Models;
using Timlog.Domain.ValueObjects;

namespace Timlog.Application.Validators;

public class SetupStepInputValidator : AbstractValidator<SetupStepInput>
{
    public SetupStepInputValidator()
    {
        When(x => x.Step == SetupStep.AwaitingAfm, () =>
        {
            RuleFor(x => x.Value)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Το ΑΦΜ δεν μπορεί να είναι κενό.")
                .Length(9).WithMessage("Το ΑΦΜ πρέπει να αποτελείται από 9 ψηφία.")
                .Matches(@"^\d{9}$").WithMessage("Το ΑΦΜ πρέπει να περιέχει μόνο αριθμούς.")
                .Must(Afm.IsValidAfmChecksum).WithMessage("Το ΑΦΜ δεν είναι έγκυρο.");
        });

        When(x => x.Step == SetupStep.AwaitingAadeUserId, () =>
        {
            RuleFor(x => x.Value)
                .NotEmpty().WithMessage("Το AADE User ID δεν μπορεί να είναι κενό.");
        });

        When(x => x.Step == SetupStep.AwaitingSubscriptionKey, () =>
        {
            RuleFor(x => x.Value)
                .NotEmpty().WithMessage("Το AADE Subscription Key δεν μπορεί να είναι κενό.");
        });

        When(x => x.Step == SetupStep.AwaitingCompanyName, () =>
        {
            RuleFor(x => x.Value)
                .NotEmpty().WithMessage("Η Επωνυμία δεν μπορεί να είναι κενή.");
        });

        When(x => x.Step == SetupStep.AwaitingPostalCode, () =>
        {
            RuleFor(x => x.Value)
                .NotEmpty().WithMessage("Ο Ταχυδρομικός Κώδικας δεν μπορεί να είναι κενός.");
        });

        When(x => x.Step == SetupStep.AwaitingCity, () =>
        {
            RuleFor(x => x.Value)
                .NotEmpty().WithMessage("Η Πόλη δεν μπορεί να είναι κενή.");
        });
    }
}
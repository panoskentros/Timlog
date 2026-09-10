using FluentValidation;
using Timlog.Application.Commands;
using Timlog.Domain.ValueObjects;

namespace Timlog.Application.Validators;

public class SetupCredentialsCommandValidator : AbstractValidator<SetupCredentialsCommand>
{
    public SetupCredentialsCommandValidator()
    {
        RuleFor(x => x.TelegramChatId)
            .GreaterThan(0)
            .WithMessage("Το Telegram Chat ID δεν είναι έγκυρο.");

        RuleFor(x => x.AadeUserId)
            .NotEmpty()
            .WithMessage("Το όνομα χρήστη της ΑΑΔΕ είναι υποχρεωτικό.");

        RuleFor(x => x.SubscriptionKey)
            .NotEmpty()
            .WithMessage("Το κλειδί συνδρομής της ΑΑΔΕ είναι υποχρεωτικό.");

        RuleFor(x => x.CompanyName)
            .NotEmpty()
            .WithMessage("Η επωνυμία της εταιρείας είναι υποχρεωτική.");

        RuleFor(x => x.PostalCode)
            .NotEmpty()
            .WithMessage("Ο ταχυδρομικός κώδικας είναι υποχρεωτικός.");

        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage("Η πόλη είναι υποχρεωτική.");

        RuleFor(x => x.IssuerAfm)
            .NotEmpty()
            .WithMessage("Το ΑΦΜ είναι υποχρεωτικό.")
            .Must(afm =>
            {
                try
                {
                    Afm.Create(afm);
                    return true;
                }
                catch
                {
                    return false;
                }
            })
            .WithMessage("Το ΑΦΜ δεν είναι έγκυρο.");
    }
}
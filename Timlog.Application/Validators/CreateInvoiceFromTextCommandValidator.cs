using FluentValidation;
using Timlog.Application.Commands;

namespace Timlog.Application.Validators;

public class CreateInvoiceFromTextCommandValidator : AbstractValidator<CreateInvoiceFromTextCommand>
{
    public CreateInvoiceFromTextCommandValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty()
            .WithMessage("Το κείμενο του τιμολογίου δεν μπορεί να είναι κενό.")
            .MinimumLength(5)
            .WithMessage("Το κείμενο πρέπει να περιέχει τουλάχιστον 5 χαρακτήρες.");
    }
}
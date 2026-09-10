using FluentValidation;
using Timlog.Domain.ValueObjects;

namespace Timlog.Application.Validators;

public class AfmValidator : AbstractValidator<string>
{
    public AfmValidator()
    {
        RuleFor(afm => afm)
            .NotEmpty()
            .WithMessage("Το ΑΦΜ δεν μπορεί να είναι κενό.")
            .Length(9)
            .WithMessage("Το ΑΦΜ πρέπει να έχει ακριβώς 9 ψηφία.")
            .Matches(@"^\d{9}$")
            .WithMessage("Το ΑΦΜ πρέπει να περιέχει μόνο αριθμούς.")
            .Must(BeValidChecksum)
            .WithMessage("Το ΑΦΜ δεν είναι έγκυρο βάσει αλγορίθμου επαλήθευσης.");
    }

    private static bool BeValidChecksum(string afm)
    {
        if (afm == "000000000") return false;

        var sum = 0;
        for (var i = 0; i < 8; i++)
        {
            sum += (afm[i] - '0') * (1 << (8 - i));
        }

        var remainder = sum % 11;
        var checkDigit = remainder == 10 ? 0 : remainder;

        return checkDigit == (afm[8] - '0');
    }
}
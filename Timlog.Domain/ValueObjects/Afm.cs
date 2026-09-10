namespace Timlog.Domain.ValueObjects;

using System.Text.RegularExpressions;

public sealed class Afm
{
    private static readonly Regex ForeignVatRegex = new(@"^[A-Z]{2}[A-Z0-9]{2,18}$", RegexOptions.Compiled);

    public string Value { get; }
    public bool IsGreek { get; }

    private Afm(string value, bool isGreek)
    {
        Value = value;
        IsGreek = isGreek;
    }

    private Afm() { }

    public static Afm Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Το ΑΦΜ δεν μπορεί να είναι κενό.");
        }

        value = value.Trim().ToUpperInvariant();

        if (value.StartsWith("EL") && value.Length == 11 && long.TryParse(value[2..], out _))
        {
            value = value[2..];
        }

        if (value.Length == 9 && long.TryParse(value, out _))
        {
            if (!IsValidAfmChecksum(value))
            {
                throw new ArgumentException("Μη έγκυρο checksum ΑΦΜ.");
            }

            return new Afm(value, true);
        }

        if (ForeignVatRegex.IsMatch(value) && !value.StartsWith("EL"))
        {
            return new Afm(value, false);
        }

        throw new ArgumentException("Μη έγκυρη μορφή ΑΦΜ.");
    }

    public static bool IsValidAfmChecksum(string afm)
    {
        int sum = 0;
        int multiplier = 256;

        for (int i = 0; i < 8; i++)
        {
            sum += (afm[i] - '0') * multiplier;
            multiplier /= 2;
        }

        int checkDigit = sum % 11;
        if (checkDigit == 10)
        {
            checkDigit = 0;
        }

        return checkDigit == (afm[8] - '0');
    }

    public override string ToString() => Value;
}
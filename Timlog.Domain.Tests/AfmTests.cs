using Timlog.Domain.ValueObjects;
using Xunit;

namespace Timlog.Domain.Tests;

public class AfmTests
{
    [Theory]
    [InlineData("090000045")]
    [InlineData("094259216")]
    [InlineData("099999999")]
    public void Create_ValidAfm_ReturnsAfmInstance(string validAfm)
    {
        var afm = Afm.Create(validAfm);

        Assert.NotNull(afm);
        Assert.Equal(validAfm, afm.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_NullOrWhitespace_ThrowsArgumentException(string? invalidAfm)
    {
        Assert.Throws<ArgumentException>(() => Afm.Create(invalidAfm!));
    }

    [Theory]
    [InlineData("12345678")]
    [InlineData("1234567890")]
    [InlineData("12345ABCD")]
    public void Create_InvalidLengthOrNonDigits_ThrowsArgumentException(string invalidAfm)
    {
        Assert.Throws<ArgumentException>(() => Afm.Create(invalidAfm));
    }

    [Theory]
    [InlineData("123456789")]
    [InlineData("090000046")]
    public void Create_InvalidChecksum_ThrowsArgumentException(string invalidChecksumAfm)
    {
        Assert.Throws<ArgumentException>(() => Afm.Create(invalidChecksumAfm));
    }
}
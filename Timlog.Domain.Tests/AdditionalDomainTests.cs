using Timlog.Domain.Enums;
using Timlog.Domain.Services;
using Timlog.Domain.ValueObjects;
using Xunit;

namespace Timlog.Domain.Tests;

public class AdditionalDomainTests
{
    [Theory]
    [InlineData("EL094259216", "094259216", true)]
    [InlineData("DE123456789", "DE123456789", false)]
    [InlineData("FR12345678901", "FR12345678901", false)]
    public void Create_PrefixedAndForeignAfm_ParsesCorrectly(string input, string expectedValue, bool expectedIsGreek)
    {
        var afm = Afm.Create(input);

        Assert.Equal(expectedValue, afm.Value);
        Assert.Equal(expectedIsGreek, afm.IsGreek);
    }

    [Fact]
    public void Calculate_ExactThreshold_DoesNotApplyWithholding()
    {
        var result = GreekTaxEngine.Calculate(300.00m, InvoiceType.ServicesDomestic);

        Assert.Equal(0.00m, result.WithholdingTaxAmount);
        Assert.Equal(372.00m, result.TotalPayableByClient);
    }

    [Fact]
    public void Calculate_AboveThreshold_WithholdingDisabled_DoesNotApplyWithholding()
    {
        var result = GreekTaxEngine.Calculate(500.00m, InvoiceType.ServicesDomestic, applyWithholding: false);

        Assert.Equal(0.00m, result.WithholdingTaxAmount);
        Assert.Equal(620.00m, result.TotalPayableByClient);
    }
}
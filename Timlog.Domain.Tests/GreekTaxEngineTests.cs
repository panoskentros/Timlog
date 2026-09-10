using Timlog.Domain.Enums;
using Timlog.Domain.Services;
using Xunit;

namespace Timlog.Domain.Tests;

public class GreekTaxEngineTests
{
    [Fact]
    public void Calculate_DomesticService_UnderThreshold_NoWithholding()
    {
        decimal netAmount = 250.00m;

        var result = GreekTaxEngine.Calculate(netAmount, InvoiceType.ServicesDomestic);

        Assert.Equal(250.00m, result.NetAmount);
        Assert.Equal(0.24m, result.VatRate);
        Assert.Equal(60.00m, result.VatAmount);
        Assert.Equal(0.00m, result.WithholdingTaxRate);
        Assert.Equal(0.00m, result.WithholdingTaxAmount);
        Assert.Equal(310.00m, result.TotalGrossAmount);
        Assert.Equal(310.00m, result.TotalPayableByClient);
        Assert.Equal(22.50m, result.EstimatedIncomeTax);
        Assert.Equal(33.25m, result.EstimatedEfka);
        Assert.Equal(194.25m, result.SafeToSpendAmount);
    }

    [Fact]
    public void Calculate_DomesticService_AtOrAboveThreshold_AppliesWithholding()
    {
        decimal netAmount = 1000.00m;

        var result = GreekTaxEngine.Calculate(netAmount, InvoiceType.ServicesDomestic);

        Assert.Equal(1000.00m, result.NetAmount);
        Assert.Equal(0.24m, result.VatRate);
        Assert.Equal(240.00m, result.VatAmount);
        Assert.Equal(0.20m, result.WithholdingTaxRate);
        Assert.Equal(200.00m, result.WithholdingTaxAmount);
        Assert.Equal(1240.00m, result.TotalGrossAmount);
        Assert.Equal(1040.00m, result.TotalPayableByClient);
        Assert.Equal(90.00m, result.EstimatedIncomeTax);
        Assert.Equal(133.00m, result.EstimatedEfka);
        Assert.Equal(777.00m, result.SafeToSpendAmount);
    }

    [Theory]
    [InlineData(InvoiceType.ServicesIntraCommunity)]
    [InlineData(InvoiceType.ServicesThirdCountry)]
    public void Calculate_ForeignServices_ZeroVatAndNoWithholding(InvoiceType type)
    {
        decimal netAmount = 1500.00m;

        var result = GreekTaxEngine.Calculate(netAmount, type);

        Assert.Equal(1500.00m, result.NetAmount);
        Assert.Equal(0.00m, result.VatRate);
        Assert.Equal(0.00m, result.VatAmount);
        Assert.Equal(0.00m, result.WithholdingTaxRate);
        Assert.Equal(0.00m, result.WithholdingTaxAmount);
        Assert.Equal(1500.00m, result.TotalGrossAmount);
        Assert.Equal(1500.00m, result.TotalPayableByClient);
    }

    [Fact]
    public void Calculate_NegativeOrZeroAmount_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            GreekTaxEngine.Calculate(0.00m, InvoiceType.ServicesDomestic));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            GreekTaxEngine.Calculate(-100.00m, InvoiceType.ServicesDomestic));
    }
}
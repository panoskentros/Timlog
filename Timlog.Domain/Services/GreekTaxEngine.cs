using Timlog.Domain.Enums;
using Timlog.Domain.Models;

namespace Timlog.Domain.Services;

public static class GreekTaxEngine
{
    private const decimal StandardVatRate = 0.24m;
    private const decimal WithholdingTaxRate = 0.20m;
    private const decimal WithholdingThreshold = 300.00m;
    private const decimal EstimatedIncomeTaxRate = 0.09m;
    private const decimal EstimatedEfkaRate = 0.133m;

    public static TaxCalculationResult Calculate(decimal netAmount, InvoiceType invoiceType, bool applyWithholding = true)
    {
        if (netAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(netAmount), "Η καθαρή αξία πρέπει να είναι μεγαλύτερη από μηδέν.");
        }

        decimal vatRate = invoiceType switch
        {
            InvoiceType.ServicesDomestic => StandardVatRate,
            InvoiceType.ServicesIntraCommunity => 0.00m,
            InvoiceType.ServicesThirdCountry => 0.00m,
            _ => throw new ArgumentOutOfRangeException(nameof(invoiceType), "Μη υποστηριζόμενος τύπος παραστατικού.")
        };

        decimal vatAmount = Math.Round(netAmount * vatRate, 2, MidpointRounding.AwayFromZero);

        decimal withholdingRateApplied = 0.00m;
        decimal withholdingAmount = 0.00m;

        if (invoiceType == InvoiceType.ServicesDomestic && applyWithholding && netAmount > WithholdingThreshold)
        {
            withholdingRateApplied = WithholdingTaxRate;
            withholdingAmount = Math.Round(netAmount * WithholdingTaxRate, 2, MidpointRounding.AwayFromZero);
        }

        decimal totalGrossAmount = netAmount + vatAmount;
        decimal totalPayableByClient = totalGrossAmount - withholdingAmount;

        decimal estimatedIncomeTax = Math.Round(netAmount * EstimatedIncomeTaxRate, 2, MidpointRounding.AwayFromZero);
        decimal estimatedEfka = Math.Round(netAmount * EstimatedEfkaRate, 2, MidpointRounding.AwayFromZero);

        decimal safeToSpend = netAmount - estimatedIncomeTax - estimatedEfka;

        return new TaxCalculationResult(
            NetAmount: netAmount,
            VatRate: vatRate,
            VatAmount: vatAmount,
            WithholdingTaxRate: withholdingRateApplied,
            WithholdingTaxAmount: withholdingAmount,
            TotalGrossAmount: totalGrossAmount,
            TotalPayableByClient: totalPayableByClient,
            EstimatedIncomeTax: estimatedIncomeTax,
            EstimatedEfka: estimatedEfka,
            SafeToSpendAmount: safeToSpend
        );
    }
}
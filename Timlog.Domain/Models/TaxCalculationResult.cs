namespace Timlog.Domain.Models;

public sealed record TaxCalculationResult(
    decimal NetAmount,
    decimal VatRate,
    decimal VatAmount,
    decimal WithholdingTaxRate,
    decimal WithholdingTaxAmount,
    decimal TotalGrossAmount,
    decimal TotalPayableByClient,
    decimal EstimatedIncomeTax,
    decimal EstimatedEfka,
    decimal SafeToSpendAmount
);
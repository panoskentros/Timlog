namespace Timlog.Application.Commands;

public record CreateInvoiceFromTextCommand(string Text);

public record CreateInvoiceResult(
    bool Success,
    string? ErrorMessage,
    Guid? InvoiceId,
    string? Mark,
    string? QrUrl,
    int? InvoiceNumber,
    string? ClientAfm,
    decimal? NetAmount,
    decimal? VatAmount,
    decimal? WithholdingAmount,
    decimal? TotalPayable,
    string? InvoiceType,
    bool WithholdingTaxRequested = false 
);
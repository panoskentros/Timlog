namespace Timlog.Application.Models;

public record ParsedInvoiceDto(
    string? ClientName,
    string? ClientAfm,
    decimal NetAmount,
    string? Description,
    int? InvoiceType,
    bool WithholdingTax
);
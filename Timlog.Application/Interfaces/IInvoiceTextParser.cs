using Timlog.Application.Models;

namespace Timlog.Application.Interfaces;

public interface IInvoiceTextParser
{
    Task<Result<ParsedInvoiceDto>> ParseAsync(string text, CancellationToken cancellationToken = default);
}
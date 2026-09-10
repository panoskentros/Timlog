using Timlog.Application.Models;
using Timlog.Domain.Entities;

namespace Timlog.Application.Interfaces;

public interface IAadeClient
{
    Task<Result<AadeOperationResult>> SendInvoiceAsync(Invoice invoice, TenantCredential credential, string aadeUserId, string subscriptionKey, CancellationToken cancellationToken = default);
    Task<Result<string>> GetInvoiceFromAadeAsync(long mark, string aadeUserId, string subscriptionKey, CancellationToken cancellationToken = default);
    Task<Result> CancelInvoiceAsync(string mark, string aadeUserId, string subscriptionKey, CancellationToken cancellationToken = default);
}

public record AadeOperationResult(string Mark, string QrUrl);
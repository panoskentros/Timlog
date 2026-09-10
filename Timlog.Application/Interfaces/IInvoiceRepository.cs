using Timlog.Domain.Entities;

namespace Timlog.Application.Interfaces;

public interface IInvoiceRepository
{
    Task<int> GetNextInvoiceNumberAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
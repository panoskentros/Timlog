using Microsoft.EntityFrameworkCore;
using Timlog.Application.Interfaces;
using Timlog.Domain.Entities;
using Timlog.Infrastructure.Data;

namespace Timlog.Infrastructure.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly TimlogDbContext _context;

    public InvoiceRepository(TimlogDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetNextInvoiceNumberAsync(CancellationToken cancellationToken = default)
    {
        var exists = await _context.Invoices.AnyAsync(cancellationToken);
        if (!exists)
        {
            return 1;
        }

        var maxNumber = await _context.Invoices.MaxAsync(i => (int?)i.InvoiceNumber, cancellationToken) ?? 0;
        return maxNumber + 1;
    }

    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        await _context.Invoices.AddAsync(invoice, cancellationToken);
    }

    public async Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
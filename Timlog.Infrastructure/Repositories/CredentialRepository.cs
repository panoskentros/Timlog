using Microsoft.EntityFrameworkCore;
using Timlog.Application.Interfaces;
using Timlog.Domain.Entities;
using Timlog.Infrastructure.Data;

namespace Timlog.Infrastructure.Repositories;

public class CredentialRepository : ICredentialRepository
{
    private readonly TimlogDbContext _context;

    public CredentialRepository(TimlogDbContext context)
    {
        _context = context;
    }

    public async Task<TenantCredential?> GetByChatIdAsync(long chatId, CancellationToken cancellationToken = default)
    {
        return await _context.TenantCredentials
            .FirstOrDefaultAsync(c => c.TelegramChatId == chatId, cancellationToken);
    }

    public async Task AddAsync(TenantCredential credential, CancellationToken cancellationToken = default)
    {
        await _context.TenantCredentials.AddAsync(credential, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
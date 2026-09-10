using Timlog.Domain.Entities;

namespace Timlog.Application.Interfaces;

public interface ICredentialRepository
{
    Task<TenantCredential?> GetByChatIdAsync(long chatId, CancellationToken cancellationToken = default);
    Task AddAsync(TenantCredential credential, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
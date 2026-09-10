using Microsoft.AspNetCore.DataProtection;
using Timlog.Application.Interfaces;

namespace Timlog.Infrastructure.Security;

public class CredentialEncryptionService : ICredentialEncryptionService
{
    private readonly IDataProtector _protector;

    public CredentialEncryptionService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Timlog.TenantCredentials.v1");
    }

    public string Encrypt(string plainText)
    {
        return _protector.Protect(plainText);
    }

    public string Decrypt(string cipherText)
    {
        return _protector.Unprotect(cipherText);
    }
}
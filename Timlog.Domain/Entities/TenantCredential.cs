using System;

namespace Timlog.Domain.Entities;

public class TenantCredential
{
    public Guid Id { get; private set; }
    public long TelegramChatId { get; private set; }
    public string IssuerAfm { get; private set; } = null!;
    public string EncryptedAadeUserId { get; private set; } = null!;
    public string EncryptedSubscriptionKey { get; private set; } = null!;
    
    public string? CompanyName { get; private set; }
    public string? Street { get; private set; }
    public string? Number { get; private set; }
    public string? PostalCode { get; private set; }
    public string? City { get; private set; }

    private TenantCredential() { }

    public TenantCredential(Guid id, long telegramChatId, string issuerAfm, string encryptedAadeUserId, string encryptedSubscriptionKey)
    {
        Id = id;
        TelegramChatId = telegramChatId;
        IssuerAfm = issuerAfm;
        EncryptedAadeUserId = encryptedAadeUserId;
        EncryptedSubscriptionKey = encryptedSubscriptionKey;
    }

    public void UpdateCredentials(string issuerAfm, string encryptedAadeUserId, string encryptedSubscriptionKey)
    {
        IssuerAfm = issuerAfm;
        EncryptedAadeUserId = encryptedAadeUserId;
        EncryptedSubscriptionKey = encryptedSubscriptionKey;
    }

    public void UpdateCompanyDetails(string? companyName, string? street, string? number, string? postalCode, string? city)
    {
        CompanyName = companyName;
        Street = street;
        Number = number;
        PostalCode = postalCode;
        City = city;
    }
}
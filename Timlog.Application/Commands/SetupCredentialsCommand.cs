namespace Timlog.Application.Commands;

public record SetupCredentialsCommand(
    long TelegramChatId,
    string IssuerAfm,
    string AadeUserId,
    string SubscriptionKey,
    string? CompanyName,
    string? Street,
    string? Number,
    string? PostalCode,
    string? City);
namespace Timlog.Application.Commands;

public enum SetupStep
{
    AwaitingAfm,
    AwaitingAadeUserId,
    AwaitingSubscriptionKey,
    AwaitingCompanyName,
    AwaitingStreet,
    AwaitingNumber,
    AwaitingPostalCode,
    AwaitingCity
}
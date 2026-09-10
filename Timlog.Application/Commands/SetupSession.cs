namespace Timlog.Application.Commands;

public class SetupSession
{
    public SetupStep CurrentStep { get; set; } = SetupStep.AwaitingAfm;
    public string? IssuerAfm { get; set; }
    public string? AadeUserId { get; set; }
    public string? SubscriptionKey { get; set; }
    public string? CompanyName { get; set; }
    public string? Street { get; set; }
    public string? Number { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
}
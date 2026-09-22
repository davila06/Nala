namespace PawTrack.Application.Subscriptions.Services;

public sealed class EntitlementLimitReachedException : Exception
{
    public EntitlementLimitReachedException(
        string entitlement,
        decimal? limit,
        decimal consumed,
        decimal remaining,
        DateTimeOffset? resetsAt)
        : base("The entitlement limit has been reached.")
    {
        Entitlement = entitlement;
        Limit = limit;
        Consumed = consumed;
        Remaining = remaining;
        ResetsAt = resetsAt;
    }

    public string Entitlement { get; }
    public decimal? Limit { get; }
    public decimal Consumed { get; }
    public decimal Remaining { get; }
    public DateTimeOffset? ResetsAt { get; }
}

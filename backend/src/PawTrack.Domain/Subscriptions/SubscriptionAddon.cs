namespace PawTrack.Domain.Subscriptions;

public sealed class SubscriptionAddon
{
    private SubscriptionAddon() { }

    public Guid Id { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public string EntitlementKey { get; private set; } = string.Empty;
    public decimal Units { get; private set; }
    public decimal? PriceCrc { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static SubscriptionAddon Create(
        Guid subscriptionId,
        string entitlementKey,
        decimal units,
        DateTimeOffset startsAt,
        DateTimeOffset expiresAt,
        decimal? priceCrc = null)
    {
        if (subscriptionId == Guid.Empty) throw new ArgumentException("Subscription is required.", nameof(subscriptionId));
        if (string.IsNullOrWhiteSpace(entitlementKey)) throw new ArgumentException("Entitlement key is required.", nameof(entitlementKey));
        if (units <= 0) throw new ArgumentOutOfRangeException(nameof(units));
        if (priceCrc is < 0) throw new ArgumentOutOfRangeException(nameof(priceCrc));
        if (expiresAt <= startsAt) throw new ArgumentException("Addon expiration must be after its start.", nameof(expiresAt));

        return new SubscriptionAddon
        {
            Id = Guid.CreateVersion7(),
            SubscriptionId = subscriptionId,
            EntitlementKey = entitlementKey.Trim(),
            Units = units,
            PriceCrc = priceCrc,
            StartsAt = startsAt,
            ExpiresAt = expiresAt,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public bool IsUsableAt(DateTimeOffset now) => IsActive && StartsAt <= now && ExpiresAt > now;
    public void Deactivate() => IsActive = false;

    public void Replace()
    {
        if (!IsActive) throw new InvalidOperationException("Only active addons can be replaced.");
        IsActive = false;
    }

    public void RenewRecurring(int months)
    {
        if (!IsActive) throw new InvalidOperationException("Only active addons can be renewed.");
        if (months <= 0) throw new ArgumentOutOfRangeException(nameof(months));
        var now = DateTimeOffset.UtcNow;
        var baseDate = ExpiresAt > now ? ExpiresAt : now;
        StartsAt = StartsAt > now ? StartsAt : now;
        ExpiresAt = baseDate.AddMonths(months);
    }
}

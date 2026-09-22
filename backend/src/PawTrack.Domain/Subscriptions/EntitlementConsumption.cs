namespace PawTrack.Domain.Subscriptions;

public sealed class EntitlementConsumption
{
    private EntitlementConsumption() { }

    public Guid Id { get; private set; }
    public Guid SubjectId { get; private set; }
    public Guid? SubscriptionId { get; private set; }
    public string EntitlementKey { get; private set; } = string.Empty;
    public decimal Units { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public DateTimeOffset CycleStart { get; private set; }
    public DateTimeOffset CycleEnd { get; private set; }
    public string? ContextType { get; private set; }
    public Guid? ContextId { get; private set; }
    public DateTimeOffset ConsumedAt { get; private set; }

    public static EntitlementConsumption Create(
        Guid subjectId,
        Guid? subscriptionId,
        string entitlementKey,
        decimal units,
        string idempotencyKey,
        DateTimeOffset cycleStart,
        DateTimeOffset cycleEnd,
        string? contextType = null,
        Guid? contextId = null)
    {
        if (subjectId == Guid.Empty) throw new ArgumentException("Subject is required.", nameof(subjectId));
        if (subscriptionId == Guid.Empty) subscriptionId = null;
        if (string.IsNullOrWhiteSpace(entitlementKey)) throw new ArgumentException("Entitlement key is required.", nameof(entitlementKey));
        if (entitlementKey.Trim().Length > 120) throw new ArgumentException("Entitlement key is too long.", nameof(entitlementKey));
        if (units <= 0) throw new ArgumentOutOfRangeException(nameof(units), "Units must be greater than zero.");
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));
        if (idempotencyKey.Trim().Length > 200) throw new ArgumentException("Idempotency key is too long.", nameof(idempotencyKey));
        if (cycleEnd <= cycleStart) throw new ArgumentException("Cycle end must be after cycle start.", nameof(cycleEnd));

        return new EntitlementConsumption
        {
            Id = Guid.CreateVersion7(),
            SubjectId = subjectId,
            SubscriptionId = subscriptionId,
            EntitlementKey = entitlementKey.Trim(),
            Units = units,
            IdempotencyKey = idempotencyKey.Trim(),
            CycleStart = cycleStart,
            CycleEnd = cycleEnd,
            ContextType = contextType?.Trim(),
            ContextId = contextId,
            ConsumedAt = DateTimeOffset.UtcNow,
        };
    }
}

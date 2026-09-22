namespace PawTrack.Domain.Subscriptions;

public sealed class PlanEntitlement
{
    private PlanEntitlement() { }

    public Guid Id { get; private set; }
    public Guid PlanId { get; private set; }
    public string EntitlementKey { get; private set; } = string.Empty;
    public EntitlementValueType ValueType { get; private set; }
    public decimal? NumericValue { get; private set; }
    public bool? BooleanValue { get; private set; }
    public string? TextValue { get; private set; }
    public string? Unit { get; private set; }
    public string? ResetPeriod { get; private set; }
    public bool IsActive { get; private set; }
    public Guid Version { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static PlanEntitlement Create(
        Guid planId,
        string entitlementKey,
        EntitlementValueType valueType,
        decimal? numericValue = null,
        bool? booleanValue = null,
        string? textValue = null,
        string? unit = null,
        string? resetPeriod = null)
    {
        if (planId == Guid.Empty) throw new ArgumentException("Plan is required.", nameof(planId));
        if (string.IsNullOrWhiteSpace(entitlementKey)) throw new ArgumentException("Entitlement key is required.", nameof(entitlementKey));
        if (entitlementKey.Trim().Length > 120) throw new ArgumentException("Entitlement key is too long.", nameof(entitlementKey));

        switch (valueType)
        {
            case EntitlementValueType.Numeric when numericValue is null or < 0:
                throw new ArgumentOutOfRangeException(nameof(numericValue), "Numeric entitlements require a non-negative value.");
            case EntitlementValueType.Boolean when booleanValue is null:
                throw new ArgumentException("Boolean entitlements require a boolean value.", nameof(booleanValue));
            case EntitlementValueType.Text when string.IsNullOrWhiteSpace(textValue):
                throw new ArgumentException("Text entitlements require a text value.", nameof(textValue));
        }

        var now = DateTimeOffset.UtcNow;
        return new PlanEntitlement
        {
            Id = Guid.CreateVersion7(),
            PlanId = planId,
            EntitlementKey = entitlementKey.Trim(),
            ValueType = valueType,
            NumericValue = numericValue,
            BooleanValue = booleanValue,
            TextValue = textValue?.Trim(),
            Unit = unit?.Trim(),
            ResetPeriod = resetPeriod?.Trim(),
            IsActive = true,
            Version = Guid.NewGuid(),
            CreatedAt = now,
            UpdatedAt = now,
        };
    }
}

using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Subscriptions.Services;

public sealed record EntitlementContext(string? ContextType = null, Guid? ContextId = null, Guid? TenantId = null);

public sealed record EntitlementValue(
    string Key,
    EntitlementValueType ValueType,
    decimal? NumericValue,
    bool? BooleanValue,
    string? TextValue,
    string? Unit,
    string? ResetPeriod,
    decimal Consumed);

public sealed record EntitlementSnapshot(
    Guid SubjectId,
    Guid? SubscriptionId,
    SubscriptionTier Tier,
    DateTimeOffset? CycleStart,
    DateTimeOffset? CycleEnd,
    IReadOnlyDictionary<string, EntitlementValue> Entitlements);

public sealed record EntitlementDecision(
    bool Allowed,
    bool Included,
    decimal? Limit,
    decimal Consumed,
    decimal Remaining,
    DateTimeOffset? ResetsAt,
    SubscriptionTier Tier);

public sealed record ConsumptionResult(
    bool Consumed,
    bool AlreadyConsumed,
    decimal ConsumedUnits,
    decimal Remaining,
    DateTimeOffset? CycleStart,
    DateTimeOffset? CycleEnd,
    DateTimeOffset? ResetsAt,
    SubscriptionTier Tier);

public interface IEntitlementService
{
    Task<EntitlementSnapshot> GetSnapshotAsync(Guid subjectId, CancellationToken cancellationToken = default);

    Task<EntitlementDecision> AuthorizeAsync(
        Guid subjectId,
        string entitlement,
        decimal requestedUnits,
        EntitlementContext context,
        CancellationToken cancellationToken = default);

    Task<ConsumptionResult> ConsumeAsync(
        Guid subjectId,
        string entitlement,
        decimal units,
        string idempotencyKey,
        EntitlementContext context,
        CancellationToken cancellationToken = default);
}

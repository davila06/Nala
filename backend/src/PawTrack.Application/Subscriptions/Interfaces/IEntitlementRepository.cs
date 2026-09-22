using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Subscriptions.Interfaces;

public interface IEntitlementRepository
{
    Task<IReadOnlyList<SubscriptionAddon>> GetActiveAddonsAsync(Guid subscriptionId, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlanEntitlement>> GetActiveForPlanAsync(Guid planId, CancellationToken cancellationToken = default);
    Task<decimal> GetConsumedAsync(
        Guid subjectId,
        string entitlementKey,
        DateTimeOffset cycleStart,
        DateTimeOffset cycleEnd,
        string? contextType = null,
        Guid? contextId = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, decimal>> GetConsumedBySubjectAsync(
        Guid subjectId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);
    Task<EntitlementConsumption?> FindByIdempotencyKeyAsync(
        Guid subjectId,
        string entitlementKey,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
    Task AddAsync(EntitlementConsumption consumption, CancellationToken cancellationToken = default);
    void Detach(EntitlementConsumption consumption);
}

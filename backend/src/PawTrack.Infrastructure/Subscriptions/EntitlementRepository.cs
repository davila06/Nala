using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Subscriptions;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Subscriptions;

public sealed class EntitlementRepository(PawTrackDbContext dbContext) : IEntitlementRepository
{
    public async Task<IReadOnlyList<SubscriptionAddon>> GetActiveAddonsAsync(
        Guid subscriptionId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default) =>
        await dbContext.SubscriptionAddons
            .AsNoTracking()
            .Where(x => x.SubscriptionId == subscriptionId && x.IsActive && x.StartsAt <= now && x.ExpiresAt > now)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PlanEntitlement>> GetActiveForPlanAsync(
        Guid planId,
        CancellationToken cancellationToken = default) =>
        await dbContext.PlanEntitlements
            .AsNoTracking()
            .Where(x => x.PlanId == planId && x.IsActive)
            .OrderBy(x => x.EntitlementKey)
            .ToListAsync(cancellationToken);

    public async Task<decimal> GetConsumedAsync(
        Guid subjectId,
        string entitlementKey,
        DateTimeOffset cycleStart,
        DateTimeOffset cycleEnd,
        string? contextType = null,
        Guid? contextId = null,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.EntitlementConsumptions
            .Where(x => x.SubjectId == subjectId
                && x.EntitlementKey == entitlementKey
                && x.CycleStart == cycleStart
                && x.CycleEnd == cycleEnd
                && x.ContextType == contextType
                && x.ContextId == contextId)
            .SumAsync(x => (decimal?)x.Units, cancellationToken)
            ?? 0m;
    }

    public Task<EntitlementConsumption?> FindByIdempotencyKeyAsync(
        Guid subjectId,
        string entitlementKey,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        dbContext.EntitlementConsumptions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SubjectId == subjectId
                && x.EntitlementKey == entitlementKey
                && x.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task<IReadOnlyDictionary<string, decimal>> GetConsumedBySubjectAsync(
        Guid subjectId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default) =>
        await dbContext.EntitlementConsumptions
            .AsNoTracking()
            .Where(x => x.SubjectId == subjectId && x.ConsumedAt >= from && x.ConsumedAt < to)
            .GroupBy(x => x.EntitlementKey)
            .Select(group => new { Key = group.Key, Units = group.Sum(x => x.Units) })
            .ToDictionaryAsync(x => x.Key, x => x.Units, StringComparer.Ordinal, cancellationToken);

    public async Task AddAsync(
        EntitlementConsumption consumption,
        CancellationToken cancellationToken = default) =>
        await dbContext.EntitlementConsumptions.AddAsync(consumption, cancellationToken);

    public void Detach(EntitlementConsumption consumption) =>
        dbContext.Entry(consumption).State = EntityState.Detached;
}

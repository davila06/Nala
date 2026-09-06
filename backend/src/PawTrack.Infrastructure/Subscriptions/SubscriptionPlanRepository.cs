using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Subscriptions;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Subscriptions;

public sealed class SubscriptionPlanRepository(PawTrackDbContext dbContext) : ISubscriptionPlanRepository
{
    public Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<SubscriptionPlan?> GetByTierAsync(SubscriptionTier tier, CancellationToken cancellationToken = default) =>
        dbContext.SubscriptionPlans.FirstOrDefaultAsync(x => x.Tier == tier, cancellationToken);

    public async Task<IReadOnlyList<SubscriptionPlan>> GetPagedAsync(
        int skip,
        int take,
        bool includeInactive,
        CancellationToken cancellationToken = default) =>
        await dbContext.SubscriptionPlans
            .Where(x => includeInactive || x.IsActive)
            .OrderBy(x => x.Tier)
            .Skip(skip)
            .Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task AddAsync(SubscriptionPlan plan, CancellationToken cancellationToken = default) =>
        await dbContext.SubscriptionPlans.AddAsync(plan, cancellationToken);

    public void Update(SubscriptionPlan plan) => dbContext.SubscriptionPlans.Update(plan);
}
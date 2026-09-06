using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Subscriptions.Interfaces;

public interface ISubscriptionPlanRepository
{
    Task<SubscriptionPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SubscriptionPlan?> GetByTierAsync(SubscriptionTier tier, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SubscriptionPlan>> GetPagedAsync(int skip, int take, bool includeInactive, CancellationToken cancellationToken = default);
    Task AddAsync(SubscriptionPlan plan, CancellationToken cancellationToken = default);
    void Update(SubscriptionPlan plan);
}
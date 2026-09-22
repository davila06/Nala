using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Subscriptions.Interfaces;

public interface ISubscriptionAddonRepository
{
    Task<SubscriptionAddon?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SubscriptionAddon>> GetForSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    Task AddAsync(SubscriptionAddon addon, CancellationToken cancellationToken = default);
    void Update(SubscriptionAddon addon);
}

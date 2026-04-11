using PawTrack.Domain.Notifications;

namespace PawTrack.Application.Common.Interfaces;

public interface IPushSubscriptionRepository
{
    Task<PushSubscription?> GetByEndpointAsync(string endpoint, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PushSubscription>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(PushSubscription subscription, CancellationToken cancellationToken = default);
    Task DeleteByEndpointAsync(string endpoint, CancellationToken cancellationToken = default);
}

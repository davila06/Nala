using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Subscriptions.Interfaces;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetActiveForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Subscription?> GetActiveForClinicAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Subscription?> GetByPaymentReferenceAsync(string reference, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Subscription>> GetPendingAsync(CancellationToken cancellationToken = default);
    /// <summary>Active subscriptions whose ExpiresAt has already passed — candidates for expiration.</summary>
    Task<IReadOnlyList<Subscription>> GetExpiredActiveAsync(CancellationToken cancellationToken = default);
    /// <summary>Active subscriptions expiring within the given number of days — candidates for renewal reminder.</summary>
    Task<IReadOnlyList<Subscription>> GetExpiringWithinAsync(int days, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Subscription>> GetAllPagedAsync(int skip, int take, CancellationToken cancellationToken = default);
    Task<int> CountAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default);
    void Update(Subscription subscription);
}

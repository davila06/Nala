using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Subscriptions.Interfaces;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetActiveForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Subscription?> GetActiveForClinicAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Subscription?> GetByPaymentReferenceAsync(string reference, CancellationToken cancellationToken = default);
    Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default);
    void Update(Subscription subscription);
}

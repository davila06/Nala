using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Subscriptions;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Subscriptions;

public sealed class SubscriptionRepository(PawTrackDbContext dbContext) : ISubscriptionRepository
{
    public Task<Subscription?> GetActiveForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.Subscriptions
            .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.ActivatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<Subscription?> GetActiveForClinicAsync(Guid clinicId, CancellationToken cancellationToken = default) =>
        dbContext.Subscriptions
            .Where(s => s.ClinicId == clinicId && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.ActivatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Subscriptions.FindAsync([id], cancellationToken).AsTask();

    public Task<Subscription?> GetByPaymentReferenceAsync(string reference, CancellationToken cancellationToken = default) =>
        dbContext.Subscriptions.FirstOrDefaultAsync(s => s.PaymentReference == reference, cancellationToken);

    public async Task<IReadOnlyList<Subscription>> GetPendingAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.PendingPayment)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Subscription>> GetAllPagedAsync(int skip, int take, CancellationToken cancellationToken = default) =>
        await dbContext.Subscriptions
            .OrderByDescending(s => s.CreatedAt)
            .Skip(skip).Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountAllAsync(CancellationToken cancellationToken = default) =>
        dbContext.Subscriptions.CountAsync(cancellationToken);

    public async Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default) =>
        await dbContext.Subscriptions.AddAsync(subscription, cancellationToken);

    public void Update(Subscription subscription) =>
        dbContext.Subscriptions.Update(subscription);
}

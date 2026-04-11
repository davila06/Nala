using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Notifications;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Notifications;

public sealed class PushSubscriptionRepository(PawTrackDbContext dbContext) : IPushSubscriptionRepository
{
    public async Task<PushSubscription?> GetByEndpointAsync(
        string endpoint,
        CancellationToken cancellationToken = default) =>
        await dbContext.PushSubscriptions
            .FirstOrDefaultAsync(x => x.Endpoint == endpoint, cancellationToken);

    public async Task<IReadOnlyList<PushSubscription>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await dbContext.PushSubscriptions
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(
        PushSubscription subscription,
        CancellationToken cancellationToken = default) =>
        await dbContext.PushSubscriptions.AddAsync(subscription, cancellationToken);

    public async Task DeleteByEndpointAsync(
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        var row = await dbContext.PushSubscriptions
            .FirstOrDefaultAsync(x => x.Endpoint == endpoint, cancellationToken);

        if (row is not null)
            dbContext.PushSubscriptions.Remove(row);
    }
}

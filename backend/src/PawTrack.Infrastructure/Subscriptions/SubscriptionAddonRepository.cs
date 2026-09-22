using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Subscriptions;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Subscriptions;

public sealed class SubscriptionAddonRepository(PawTrackDbContext dbContext) : ISubscriptionAddonRepository
{
    public Task<SubscriptionAddon?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.SubscriptionAddons.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SubscriptionAddon>> GetForSubscriptionAsync(
        Guid subscriptionId, CancellationToken cancellationToken = default) =>
        await dbContext.SubscriptionAddons.AsNoTracking()
            .Where(x => x.SubscriptionId == subscriptionId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(SubscriptionAddon addon, CancellationToken cancellationToken = default) =>
        await dbContext.SubscriptionAddons.AddAsync(addon, cancellationToken);

    public void Update(SubscriptionAddon addon) => dbContext.SubscriptionAddons.Update(addon);
}

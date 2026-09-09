using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Webhooks;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Webhooks;

public sealed class WebhookRepository(PawTrackDbContext db) : IWebhookRepository
{
    public async Task AddSubscriptionAsync(WebhookSubscription subscription, CancellationToken ct = default) => await db.WebhookSubscriptions.AddAsync(subscription, ct);
    public Task<WebhookSubscription?> GetByIdAsync(Guid id, CancellationToken ct = default) => db.WebhookSubscriptions.FirstOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyList<WebhookSubscription>> GetActiveForEventAsync(string eventType, CancellationToken ct = default) =>
        await db.WebhookSubscriptions.AsNoTracking().Where(x => x.IsActive && x.EventTypesJson.Contains($"\"{eventType}\"")).Take(100).ToListAsync(ct);
    public async Task AddDeliveryAsync(WebhookDelivery delivery, CancellationToken ct = default) => await db.WebhookDeliveries.AddAsync(delivery, ct);
    public async Task<IReadOnlyList<WebhookDelivery>> GetPendingDeliveriesAsync(int take = 50, CancellationToken ct = default) =>
        await db.WebhookDeliveries.Where(x => x.Status == WebhookDeliveryStatus.Pending && x.NextAttemptAt <= DateTimeOffset.UtcNow)
            .OrderBy(x => x.NextAttemptAt).Take(Math.Clamp(take, 1, 200)).ToListAsync(ct);
    public Task<WebhookSubscription?> GetSubscriptionForDeliveryAsync(Guid subscriptionId, CancellationToken ct = default) =>
        db.WebhookSubscriptions.FirstOrDefaultAsync(x => x.Id == subscriptionId, ct);
    public void UpdateDelivery(WebhookDelivery delivery) => db.WebhookDeliveries.Update(delivery);
}
using PawTrack.Domain.Webhooks;

namespace PawTrack.Application.Common.Interfaces;

public interface IWebhookRepository
{
    Task AddSubscriptionAsync(WebhookSubscription subscription, CancellationToken ct = default);
    Task<WebhookSubscription?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<WebhookSubscription>> GetActiveForEventAsync(string eventType, CancellationToken ct = default);
    Task AddDeliveryAsync(WebhookDelivery delivery, CancellationToken ct = default);
    Task<IReadOnlyList<WebhookDelivery>> GetPendingDeliveriesAsync(int take = 50, CancellationToken ct = default);
    Task<WebhookSubscription?> GetSubscriptionForDeliveryAsync(Guid subscriptionId, CancellationToken ct = default);
    void UpdateDelivery(WebhookDelivery delivery);
}
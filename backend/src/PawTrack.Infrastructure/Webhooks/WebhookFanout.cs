using System.Text.Json;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Webhooks;
using PawTrack.Infrastructure.Observability;

namespace PawTrack.Infrastructure.Webhooks;

public sealed class WebhookFanout(
    IWebhookRepository repository,
    IDataProtectionService protection,
    IUnitOfWork unitOfWork) : IWebhookFanout
{
    public async Task QueueAsync(string eventType, string payload, CancellationToken ct = default)
    {
        var subscriptions = await repository.GetActiveForEventAsync(eventType, ct);
        foreach (var subscription in subscriptions)
        {
            var secret = protection.Unprotect(subscription.SecretProtected);
            await repository.AddDeliveryAsync(WebhookDelivery.Create(subscription.Id, eventType, payload, DateTimeOffset.UtcNow, secret), ct);
            EnterpriseMetrics.WebhookQueued.Add(1, new KeyValuePair<string, object?>("event_type", eventType));
        }
        if (subscriptions.Count > 0) await unitOfWork.SaveChangesAsync(ct);
    }
}
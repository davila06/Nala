using System.Security.Cryptography;
using System.Text;

namespace PawTrack.Domain.Webhooks;

public enum WebhookDeliveryStatus { Pending, Delivered, Failed, Disabled }

public sealed class WebhookDelivery
{
    private WebhookDelivery() { }
    public Guid Id { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string Signature { get; private set; } = string.Empty;
    public int AttemptCount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public string? LastError { get; private set; }
    public WebhookDeliveryStatus Status { get; private set; }

    public static WebhookDelivery Create(Guid subscriptionId, string eventType, string payload, DateTimeOffset now, string? secret = null)
    {
        var idempotencyKey = Guid.CreateVersion7().ToString();
        return new WebhookDelivery
        {
            Id = Guid.CreateVersion7(),
            SubscriptionId = subscriptionId,
            EventType = eventType.Trim(),
            Payload = payload,
            IdempotencyKey = idempotencyKey,
            Signature = Sign(secret ?? "development-only", payload),
            AttemptCount = 0,
            CreatedAt = now,
            NextAttemptAt = now,
            Status = WebhookDeliveryStatus.Pending,
        };
    }

    public void MarkDelivered(DateTimeOffset now) { Status = WebhookDeliveryStatus.Delivered; DeliveredAt = now; }
    public void MarkFailed(string error)
    {
        AttemptCount++;
        LastError = error.Length > 1000 ? error[..1000] : error;
        Status = AttemptCount >= 8 ? WebhookDeliveryStatus.Failed : WebhookDeliveryStatus.Pending;
        NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(Math.Min(3600, Math.Pow(2, AttemptCount) * 5));
    }

    public static string Sign(string secret, string payload) =>
        "sha256=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
}
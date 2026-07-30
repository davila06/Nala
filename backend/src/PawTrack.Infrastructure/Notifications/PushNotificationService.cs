using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using WebPush;

namespace PawTrack.Infrastructure.Notifications;

/// <summary>
/// Delivers Web Push notifications directly to browser push servers via VAPID.
/// No paid provider required — uses the free RFC 8030 Web Push Protocol.
/// Set Notifications:Push:VapidPublicKey and Notifications:Push:VapidPrivateKey in config.
/// </summary>
public sealed class PushNotificationService(
    IConfiguration configuration,
    IServiceScopeFactory scopeFactory,
    ILogger<PushNotificationService> logger)
    : IPushNotificationService
{
    private readonly string? _vapidPublicKey = configuration["Notifications:Push:VapidPublicKey"];
    private readonly string? _vapidPrivateKey = configuration["Notifications:Push:VapidPrivateKey"];
    private readonly string _vapidSubject = configuration["Notifications:Push:VapidSubject"]
        ?? "mailto:ops@pawtrack.cr";

    public async Task SendAsync(
        Guid userId,
        string title,
        string body,
        PushNotificationMetadata? metadata = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_vapidPublicKey) || string.IsNullOrWhiteSpace(_vapidPrivateKey))
        {
            logger.LogInformation(
                "Push skipped (VAPID not configured). User={UserId} [{Title}] {Body}",
                userId, title, body);
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IPushSubscriptionRepository>();

        var subscriptions = await repo.GetByUserIdAsync(userId, cancellationToken);
        if (subscriptions.Count == 0) return;

        var payload = JsonSerializer.Serialize(new PushPayload(
            title, body, metadata?.Url, metadata?.ResolveCheckNotificationId));

        var client = new WebPushClient();
        client.SetVapidDetails(_vapidSubject, _vapidPublicKey, _vapidPrivateKey);

        foreach (var sub in subscriptions)
        {
            try
            {
                var keys = JsonSerializer.Deserialize<PushKeys>(sub.KeysJson);
                if (keys is null) continue;

                var pushSub = new PushSubscription(sub.Endpoint, keys.P256dh, keys.Auth);
                await client.SendNotificationAsync(pushSub, payload);
            }
            catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone
                                           || ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Subscription expired — remove it
                await repo.DeleteByEndpointAsync(sub.Endpoint, cancellationToken);
                logger.LogInformation("Removed stale push subscription for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send push to user {UserId}", userId);
            }
        }
    }

    private sealed record PushPayload(
        string title,
        string body,
        string? url,
        string? resolveCheckNotificationId);

    private sealed record PushKeys(string? p256dh, string? auth)
    {
        public string? P256dh => p256dh;
        public string? Auth => auth;
    }
}

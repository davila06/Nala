using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Infrastructure.Notifications;

/// <summary>
/// Push notification integration.
/// When Notifications:Push:ProviderUrl is configured, sends notifications through
/// that HTTP provider endpoint using a JSON payload.
/// Otherwise falls back to structured logs (development-safe).
/// </summary>
public sealed class PushNotificationService(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<PushNotificationService> logger)
    : IPushNotificationService
{
    private readonly string? _providerUrl = configuration["Notifications:Push:ProviderUrl"];
    private readonly string? _apiKey = configuration["Notifications:Push:ApiKey"];
    private readonly bool _enabled =
        bool.TryParse(configuration["Notifications:Push:Enabled"], out var enabled)
        && enabled;

    public async Task SendAsync(
        Guid userId,
        string title,
        string body,
        PushNotificationMetadata? metadata = null,
        CancellationToken cancellationToken = default)
    {
        if (_enabled && !string.IsNullOrWhiteSpace(_providerUrl))
        {
            try
            {
                var client = httpClientFactory.CreateClient("PushProvider");
                using var request = new HttpRequestMessage(HttpMethod.Post, _providerUrl)
                {
                    Content = JsonContent.Create(new PushProviderRequest(
                        userId,
                        title,
                        body,
                        metadata?.Url,
                        metadata?.ResolveCheckNotificationId,
                        metadata?.Category,
                        metadata?.ActionIds,
                        DateTimeOffset.UtcNow)),
                };

                if (!string.IsNullOrWhiteSpace(_apiKey))
                    request.Headers.Add("X-Api-Key", _apiKey);

                var response = await client.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning(
                        "Push provider rejected notification for user {UserId}. Status={StatusCode}",
                        userId,
                        (int)response.StatusCode);
                }

                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Push provider call failed for user {UserId}", userId);
            }
        }

        logger.LogInformation(
            "Push fallback notification to user {UserId}: [{Title}] {Body}. Url={Url} ResolveCheckNotificationId={ResolveId}",
            userId,
            title,
            body,
            metadata?.Url,
            metadata?.ResolveCheckNotificationId);

        await Task.CompletedTask;
    }

    private sealed record PushProviderRequest(
        Guid UserId,
        string Title,
        string Body,
        string? Url,
        string? ResolveCheckNotificationId,
        string? Category,
        IReadOnlyList<string>? ActionIds,
        DateTimeOffset SentAtUtc);
}

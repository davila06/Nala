using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Webhooks;
using PawTrack.Infrastructure.Observability;
using System.Diagnostics;

namespace PawTrack.Infrastructure.Webhooks;

public sealed class OutboundWebhookHostedService(
    IServiceScopeFactory scopeFactory,
    IDistributedJobLock jobLock,
    IHttpClientFactory httpClientFactory,
    ILogger<OutboundWebhookHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            await using var lease = await jobLock.TryAcquireAsync("OutboundWebhooks", TimeSpan.FromSeconds(30), stoppingToken);
            if (lease is null) continue;
            try { await ProcessAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Outbound webhook batch failed."); }
        }
    }

    private async Task ProcessAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IWebhookRepository>();
        var protection = scope.ServiceProvider.GetRequiredService<IDataProtectionService>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var client = httpClientFactory.CreateClient("OutboundWebhooks");
        foreach (var delivery in await repo.GetPendingDeliveriesAsync(50, ct))
        {
            var stopwatch = Stopwatch.StartNew();
            var subscription = await repo.GetSubscriptionForDeliveryAsync(delivery.SubscriptionId, ct);
            if (subscription is null || !subscription.IsActive) { delivery.MarkFailed("Subscription inactive."); repo.UpdateDelivery(delivery); continue; }
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, subscription.EndpointUrl)
                { Content = new StringContent(delivery.Payload, Encoding.UTF8, "application/json") };
                request.Headers.Add("X-PawTrack-Event", delivery.EventType);
                request.Headers.Add("X-PawTrack-Delivery", delivery.IdempotencyKey);
                request.Headers.Add("X-PawTrack-Signature", WebhookDelivery.Sign(protection.Unprotect(subscription.SecretProtected), delivery.Payload));
                using var response = await client.SendAsync(request, ct);
                if (response.IsSuccessStatusCode)
                {
                    delivery.MarkDelivered(DateTimeOffset.UtcNow);
                    EnterpriseMetrics.WebhookDelivered.Add(1);
                }
                else
                {
                    delivery.MarkFailed($"HTTP {(int)response.StatusCode}");
                    EnterpriseMetrics.WebhookFailed.Add(1, new KeyValuePair<string, object?>("status", (int)response.StatusCode));
                }
            }
            catch (Exception ex) { delivery.MarkFailed(ex.Message); EnterpriseMetrics.WebhookFailed.Add(1); }
            finally { EnterpriseMetrics.WebhookDeliveryDurationMs.Record(stopwatch.Elapsed.TotalMilliseconds); }
            repo.UpdateDelivery(delivery);
        }
        await uow.SaveChangesAsync(ct);
    }
}
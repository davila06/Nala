using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Outbox;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Outbox;

/// <summary>
/// Polls the OutboxMessages table every 10 seconds and re-publishes pending messages via MediatR.
/// Provides at-least-once delivery: if the process died after commit but before in-process dispatch,
/// this processor ensures the event is eventually delivered.
/// </summary>
public sealed class OutboxProcessorHostedService(
    IServiceScopeFactory scopeFactory,
    IDistributedJobLock jobLock,
    ILogger<OutboxProcessorHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(10);
    private const int BatchSize = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(PollingInterval, stoppingToken);

            // Only one instance processes the outbox at a time.
            await using var lease = await jobLock.TryAcquireAsync("OutboxProcessor", TimeSpan.FromSeconds(30), stoppingToken);
            if (lease is null) continue;

            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "OutboxProcessor batch failed");
            }
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PawTrackDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var messages = await db.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Pending)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (messages.Count == 0) return;

        logger.LogDebug("OutboxProcessor: processing {Count} pending messages", messages.Count);

        foreach (var msg in messages)
        {
            try
            {
                var type = Type.GetType(msg.MessageType);
                if (type is null)
                {
                    msg.MarkFailed($"Type not found: {msg.MessageType}");
                    continue;
                }

                var payload = JsonSerializer.Deserialize(msg.Payload, type);
                if (payload is null)
                {
                    msg.MarkFailed("Payload deserialization returned null");
                    continue;
                }

                await publisher.Publish(payload, ct);
                msg.MarkProcessed();
            }
            catch (Exception ex)
            {
                msg.MarkFailed(ex.Message);
                logger.LogWarning(ex, "OutboxProcessor: failed to deliver message {Id} type={Type}", msg.Id, msg.MessageType);
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogDebug("OutboxProcessor: batch done — {Processed} delivered", messages.Count(m => m.Status == OutboxMessageStatus.Processed));
    }
}

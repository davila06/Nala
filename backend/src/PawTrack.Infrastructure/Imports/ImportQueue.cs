using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Imports;

namespace PawTrack.Infrastructure.Imports;

public sealed class ImportQueue : BackgroundService, IImportQueue
{
    private readonly Channel<ImportWorkItem> channel = Channel.CreateBounded<ImportWorkItem>(20);
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<ImportQueue> logger;

    public ImportQueue(IServiceScopeFactory scopeFactory, ILogger<ImportQueue> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    public ValueTask<bool> EnqueueAsync(ImportWorkItem item, CancellationToken cancellationToken = default) =>
        channel.Writer.WaitToWriteAsync(cancellationToken).IsCompletedSuccessfully
            ? new ValueTask<bool>(channel.Writer.TryWrite(item))
            : EnqueueSlowAsync(item, cancellationToken);

    private async ValueTask<bool> EnqueueSlowAsync(ImportWorkItem item, CancellationToken ct)
    {
        await channel.Writer.WriteAsync(item, ct);
        return true;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<StoreProductImportProcessor>();
                if (item.ResourceType == "StoreProducts")
                    await processor.ProcessAsync(item.TenantId, item.Format, item.FileHash, item.IdempotencyKey, item.Payload, stoppingToken);
                else
                    await processor.ProcessProviderServicesAsync(item.TenantId, item.Format, item.FileHash, item.IdempotencyKey, item.Payload, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Import job failed for tenant {TenantId}", item.TenantId);
            }
        }
    }
}

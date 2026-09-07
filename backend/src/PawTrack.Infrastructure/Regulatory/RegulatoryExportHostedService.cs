using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Regulatory.Commands;
using PawTrack.Application.Regulatory.Interfaces;
using PawTrack.Domain.Audit;
using Microsoft.ApplicationInsights;

namespace PawTrack.Infrastructure.Regulatory;

public sealed class RegulatoryExportHostedService(
    IServiceScopeFactory scopeFactory,
    IDistributedJobLock jobLock,
    ILogger<RegulatoryExportHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            await ProcessBatchAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var lease = await jobLock.TryAcquireAsync("RegulatoryExportGeneration", LockDuration, cancellationToken);
        if (lease is null) return;

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRegulatoryExportRepository>();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var blobStorage = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();
            var auditLogRepository = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var exports = await repository.GetPendingAsync(10, cancellationToken);

            foreach (var export in exports)
            {
                var result = await sender.Send(new GenerateRegulatoryExportCommand(export.Id, export.RequestedByUserId), cancellationToken);
                if (result.IsFailure)
                    logger.LogWarning("Regulatory export {ExportId} failed: {ErrorCode}", export.Id, result.Errors.FirstOrDefault());
            }

            var expiredExports = await repository.GetExpiredAsync(DateTimeOffset.UtcNow, 50, cancellationToken);
            foreach (var export in expiredExports)
            {
                if (!string.IsNullOrWhiteSpace(export.BlobUrl))
                    await blobStorage.DeleteAsync(export.BlobUrl, cancellationToken);

                var expiration = export.Expire(DateTimeOffset.UtcNow);
                if (expiration.IsFailure) continue;
                repository.Update(export);
                await auditLogRepository.AddAsync(AuditLogEntry.Create(
                    export.RequestedByUserId,
                    AuditAction.RegulatoryExportExpired,
                    "RegulatoryExport",
                    export.Id.ToString()), cancellationToken);
                logger.LogInformation("RegulatoryExport.Expired {ExportId}", export.Id);
            }

            if (expiredExports.Count > 0)
                await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Regulatory export generation batch failed");
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;

namespace PawTrack.Infrastructure.SearchCoordination;

public sealed class SearchLocationSharingExpirationHostedService(
    IServiceScopeFactory scopeFactory,
    IDistributedJobLock jobLock,
    ILogger<SearchLocationSharingExpirationHostedService> logger,
    IHostEnvironment environment)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (environment.IsEnvironment("Testing"))
            return;

        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await using var lease = await jobLock.TryAcquireAsync(
                "SearchLocationSharingExpiration", TimeSpan.FromMinutes(2), stoppingToken);
            if (lease is null) continue;

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var repository = scope.ServiceProvider
                    .GetRequiredService<ISearchLocationSharingSessionRepository>();
                var auditLog = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var now = DateTimeOffset.UtcNow;
                var sessions = await repository.GetExpiredActiveAsync(now, stoppingToken);

                foreach (var session in sessions)
                {
                    session.Expire(now);
                    repository.Update(session);
                    await auditLog.AddAsync(
                        AuditLogEntry.Create(
                            session.UserId,
                            AuditAction.SearchLocationSharingExpired,
                            "SearchLocationSharing",
                            session.LostEventId.ToString()),
                        stoppingToken);
                }

                if (sessions.Count > 0)
                    await unitOfWork.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Search location sharing expiration failed.");
            }
        }
    }
}

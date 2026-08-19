using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Auth;

/// <summary>Deletes expired RevokedToken rows nightly to prevent unbounded table growth.</summary>
public sealed class RevokedTokenCleanupJob(
    IServiceScopeFactory scopeFactory,
    ILogger<RevokedTokenCleanupJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await PurgeExpiredAsync(stoppingToken);
        }
    }

    private async Task PurgeExpiredAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PawTrackDbContext>();
            var deleted = await db.RevokedTokens
                .Where(r => r.ExpiresAt <= DateTimeOffset.UtcNow)
                .ExecuteDeleteAsync(ct);
            if (deleted > 0)
                logger.LogInformation("[RevokedTokenCleanup] Purged {Count} expired JTI entries", deleted);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "[RevokedTokenCleanup] Failed to purge expired tokens");
        }
    }
}

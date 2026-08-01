using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Collars;

/// <summary>
/// Deletes CollarLocation records older than 30 days at 03:00 UTC daily.
/// Keeps the table bounded without requiring manual intervention.
/// </summary>
public sealed class CollarLocationPurgeJob(
    IServiceProvider services,
    ILogger<CollarLocationPurgeJob> logger) : BackgroundService
{
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run once on startup after initial delay, then every 24 hours at ~03:00 UTC
        await DelayUntilNextRun(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        do
        {
            await PurgeAsync(stoppingToken);
        }
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task PurgeAsync(CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PawTrackDbContext>();

        var cutoff = DateTimeOffset.UtcNow - RetentionPeriod;
        var deleted = await db.CollarLocations
            .Where(l => l.RecordedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted > 0)
            logger.LogInformation("Purged {Count} CollarLocation records older than {Days} days.", deleted, RetentionPeriod.Days);
    }

    private static async Task DelayUntilNextRun(CancellationToken stoppingToken)
    {
        var now   = DateTime.UtcNow;
        var next  = now.Date.AddDays(1).AddHours(3); // next 03:00 UTC
        var delay = next - now;
        if (delay > TimeSpan.Zero)
            await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
    }
}

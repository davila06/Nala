using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Common.Settings;

namespace PawTrack.Infrastructure.Notifications.Jobs;

/// <summary>
/// Deletes QR-scan event records older than the configured retention window (default 90 days).
/// Runs once daily via <see cref="QrScanRetentionHostedService"/>.
/// </summary>
public sealed class QrScanRetentionJob(
    IQrScanEventRepository qrScanEventRepository,
    IUnitOfWork unitOfWork,
    IOptions<QrScanRetentionSettings> settings,
    ILogger<QrScanRetentionJob> logger)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var retentionDays = settings.Value.RetentionDays;
        var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays);

        logger.LogInformation(
            "QrScanRetentionJob: purging records scanned before {Cutoff} (retention={Days}d)",
            cutoff, retentionDays);

        var deleted = await qrScanEventRepository.DeleteBeforeAsync(cutoff, cancellationToken);

        if (deleted > 0)
        {
            // DeleteBeforeAsync uses ExecuteDeleteAsync which bypasses the change tracker,
            // so SaveChangesAsync here only commits any other pending changes.
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("QrScanRetentionJob finished. Deleted={Deleted} records.", deleted);
    }
}

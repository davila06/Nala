using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Common.Settings;

namespace PawTrack.Infrastructure.Compliance;

/// <summary>
/// Purges personal data categories that have no automatic short-term expiry —
/// sightings, closed chat threads, read notifications, and product events — once they exceed the
/// configured retention window. Runs once daily via
/// <see cref="PersonalDataRetentionHostedService"/>.
/// Implements the Ley 8968 (Costa Rica) proportional conservation principle.
/// </summary>
public sealed class PersonalDataRetentionJob(
    ISightingRepository sightingRepository,
    IChatRepository chatRepository,
    INotificationRepository notificationRepository,
    IProductEventRepository productEventRepository,
    IMedicalRepository medicalRepository,
    IClinicMedicalExportRepository clinicMedicalExportRepository,
    IUnitOfWork unitOfWork,
    IOptions<PersonalDataRetentionSettings> settings,
    ILogger<PersonalDataRetentionJob> logger)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var config = settings.Value;

        var sightingCutoff = now.AddDays(-config.SightingRetentionDays);
        var deletedSightings = await sightingRepository.DeleteReportedBeforeAsync(sightingCutoff, cancellationToken);

        var chatCutoff = now.AddDays(-config.ClosedChatRetentionDays);
        var deletedThreads = await chatRepository.DeleteClosedThreadsOlderThanAsync(chatCutoff, cancellationToken);

        var notificationCutoff = now.AddDays(-config.ReadNotificationRetentionDays);
        var deletedNotifications = await notificationRepository.DeleteReadBeforeAsync(notificationCutoff, cancellationToken);

        var productEventCutoff = now.AddDays(-config.ProductEventRetentionDays);
        var deletedProductEvents = await productEventRepository.DeleteOccurredBeforeAsync(productEventCutoff, cancellationToken);

        var medicalCutoff = now.AddDays(-config.SupersededMedicalRecordRetentionDays);
        var deletedMedicalVersions = await medicalRepository.DeleteSupersededBeforeAsync(medicalCutoff, cancellationToken);
        var expiredExports = await clinicMedicalExportRepository.ExpireBeforeAsync(now, cancellationToken);
        var exportMetadataCutoff = now.AddDays(-config.ExpiredClinicExportRetentionDays);
        var deletedExportMetadata = await clinicMedicalExportRepository.DeleteExpiredBeforeAsync(exportMetadataCutoff, cancellationToken);

        if (deletedSightings > 0 || deletedThreads > 0 || deletedNotifications > 0 || deletedProductEvents > 0 || deletedMedicalVersions > 0 || expiredExports > 0 || deletedExportMetadata > 0)
        {
            // All three deletes use ExecuteDeleteAsync which bypasses the change tracker,
            // so SaveChangesAsync here only commits any other pending changes.
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation(
            "PersonalDataRetentionJob finished. Sightings={Sightings} ChatThreads={Threads} Notifications={Notifications} ProductEvents={ProductEvents} MedicalVersions={MedicalVersions} ExpiredExports={ExpiredExports} ExportMetadata={ExportMetadata}",
            deletedSightings, deletedThreads, deletedNotifications, deletedProductEvents, deletedMedicalVersions, expiredExports, deletedExportMetadata);
    }
}

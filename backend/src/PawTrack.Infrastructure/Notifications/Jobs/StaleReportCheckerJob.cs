using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Notifications;
using Microsoft.Extensions.Options;
using PawTrack.Application.Common.Settings;

namespace PawTrack.Infrastructure.Notifications.Jobs;

/// <summary>
/// Scans active lost reports and creates reminder notifications when reports look stale:
/// active for 30+ days, no sightings in the last 30 days, and no reminder in the last 7 days.
/// </summary>
public sealed class StaleReportCheckerJob(
    ILostPetRepository lostPetRepository,
    ISightingRepository sightingRepository,
    INotificationRepository notificationRepository,
    IPetRepository petRepository,
    IUserRepository userRepository,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    IOptions<ResolveCheckSettings> settings,
    ILogger<StaleReportCheckerJob> logger)
{

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var cfg = settings.Value;
        var staleThreshold   = TimeSpan.FromDays(cfg.StaleDays);
        var reminderCooldown = TimeSpan.FromDays(cfg.ReminderCooldownDays);

        var nowUtc = DateTimeOffset.UtcNow;
        var staleBefore = nowUtc.Subtract(staleThreshold);

        var candidates = await lostPetRepository.GetActiveReportedBeforeAsync(staleBefore, cancellationToken);
        if (candidates.Count == 0)
        {
            logger.LogDebug("StaleReportCheckerJob: no active reports older than 30 days.");
            return;
        }

        var remindersCreated = 0;
        foreach (var report in candidates)
        {
            var hasRecentSighting = await sightingRepository.HasSightingsForLostEventSinceAsync(
                report.Id,
                nowUtc.Subtract(staleThreshold),
                cancellationToken);

            if (hasRecentSighting)
                continue;

            var relatedEntityId = report.Id.ToString();
            var hasRecentReminder = await notificationRepository.HasRecentByUserTypeAndEntityAsync(
                report.OwnerId,
                NotificationType.StaleReportReminder,
                relatedEntityId,
                reminderCooldown,
                cancellationToken);

            if (hasRecentReminder)
                continue;

            var pet = await petRepository.GetByIdAsync(report.PetId, cancellationToken);
            var owner = await userRepository.GetByIdAsync(report.OwnerId, cancellationToken);
            if (pet is null || owner is null)
                continue;

            var notification = Notification.Create(
                owner.Id,
                NotificationType.StaleReportReminder,
                $"¿{pet.Name} sigue perdido?",
                "No hemos visto actividad reciente en tu reporte. Confirma si sigue activo o márcalo como reunido.",
                relatedEntityId);

            await notificationRepository.AddAsync(notification, cancellationToken);
            await emailSender.SendStaleReportReminderAsync(owner.Email, owner.Name, pet.Name, cancellationToken);
            remindersCreated++;
        }

        if (remindersCreated > 0)
            await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "StaleReportCheckerJob finished. Candidates={Candidates}, Reminders={Reminders}",
            candidates.Count,
            remindersCreated);
    }
}

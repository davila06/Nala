using Microsoft.Extensions.Logging;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;
using PawTrack.Domain.Notifications;

namespace PawTrack.Application.Collars.Services;

/// <summary>
/// Core logic for the offline-detection and low-battery alert background jobs.
/// Extracted from the hosted-service wrappers so it can be unit tested without
/// standing up a <c>BackgroundService</c> host.
/// </summary>
public sealed class CollarConnectivityAlertService(
    ICollarRepository collarRepository,
    IPetRepository petRepository,
    INotificationRepository notificationRepository,
    IPushNotificationService pushNotificationService,
    IUnitOfWork unitOfWork,
    ILogger<CollarConnectivityAlertService> logger)
{
    /// <summary>Collars that have not reported within their configured threshold are marked offline once per outage.</summary>
    private static readonly TimeSpan OfflineAlertCooldown = TimeSpan.FromHours(6);

    /// <summary>Battery alerts are capped to at most one per collar per day.</summary>
    private static readonly TimeSpan BatteryAlertCooldown = TimeSpan.FromHours(24);

    public async Task RunOfflineDetectionAsync(CancellationToken cancellationToken)
    {
        var candidates = await collarRepository.GetActiveCollarsWithAlertsEnabledAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var newlyOffline = 0;

        foreach (var collar in candidates)
        {
            if (!collar.OfflineAlertsEnabled || collar.IsOffline)
                continue;
            if (collar.LastSeenAt is null)
                continue;

            var offlineFor = now - collar.LastSeenAt.Value;
            if (offlineFor < TimeSpan.FromMinutes(collar.OfflineThresholdMinutes))
                continue;

            collar.MarkOffline();
            collarRepository.Update(collar);
            newlyOffline++;

            await NotifyOfflineAsync(collar, cancellationToken);
        }

        if (newlyOffline > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("CollarConnectivityAlertService: marked {Count} collar(s) offline.", newlyOffline);
        }
    }

    public async Task RunBatteryAlertDetectionAsync(CancellationToken cancellationToken)
    {
        var candidates = await collarRepository.GetActiveCollarsWithAlertsEnabledAsync(cancellationToken);
        var alertsSent = 0;

        foreach (var collar in candidates)
        {
            if (!collar.BatteryAlertsEnabled || collar.BatteryPercent is null)
                continue;
            if (collar.BatteryPercent.Value > collar.BatteryAlertThresholdPercent)
                continue;

            var alreadyAlerted = await notificationRepository.HasRecentByUserTypeAndEntityAsync(
                collar.OwnerId, NotificationType.CollarLowBatteryAlert, collar.Id.ToString(),
                BatteryAlertCooldown, cancellationToken);
            if (alreadyAlerted)
                continue;

            await NotifyLowBatteryAsync(collar, cancellationToken);
            alertsSent++;
        }

        if (alertsSent > 0)
            logger.LogInformation("CollarConnectivityAlertService: sent {Count} low-battery alert(s).", alertsSent);
    }

    private async Task NotifyOfflineAsync(Collar collar, CancellationToken cancellationToken)
    {
        var alreadyAlerted = await notificationRepository.HasRecentByUserTypeAndEntityAsync(
            collar.OwnerId, NotificationType.CollarOfflineAlert, collar.Id.ToString(),
            OfflineAlertCooldown, cancellationToken);
        if (alreadyAlerted)
            return;

        var pet = await petRepository.GetByIdAsync(collar.PetId, cancellationToken);
        var petName = pet?.Name ?? "tu mascota";
        var hours = collar.OfflineThresholdMinutes / 60;
        var title = "Collar sin conexión";
        var body = hours >= 1
            ? $"El collar de {petName} no reporta ubicación desde hace más de {hours}h."
            : $"El collar de {petName} no reporta ubicación desde hace más de {collar.OfflineThresholdMinutes} min.";

        var notification = Notification.Create(
            collar.OwnerId, NotificationType.CollarOfflineAlert, title, body, collar.Id.ToString());
        await notificationRepository.AddAsync(notification, cancellationToken);
        await pushNotificationService.SendAsync(collar.OwnerId, title, body, cancellationToken: cancellationToken);
    }

    private async Task NotifyLowBatteryAsync(Collar collar, CancellationToken cancellationToken)
    {
        var pet = await petRepository.GetByIdAsync(collar.PetId, cancellationToken);
        var petName = pet?.Name ?? "tu mascota";
        var title = "Batería baja del collar";
        var body = $"El collar de {petName} tiene {collar.BatteryPercent}% de batería. Cárgalo pronto.";

        var notification = Notification.Create(
            collar.OwnerId, NotificationType.CollarLowBatteryAlert, title, body, collar.Id.ToString());
        await notificationRepository.AddAsync(notification, cancellationToken);
        await pushNotificationService.SendAsync(collar.OwnerId, title, body, cancellationToken: cancellationToken);
    }
}

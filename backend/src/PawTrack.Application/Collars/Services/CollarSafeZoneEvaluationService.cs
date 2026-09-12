using Microsoft.Extensions.Logging;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;
using PawTrack.Domain.Notifications;

namespace PawTrack.Application.Collars.Services;

/// <summary>
/// Evaluates a new collar position against its enabled safe zones and dispatches a
/// notification on breach (inside → outside transition). Called inline from the
/// location-ingest paths (device push, manual record, Tractive poll) instead of a
/// separate polling job — a new fix is already the trigger, so re-checking it against
/// zones adds negligible cost and reacts immediately instead of on the next poll cycle.
/// </summary>
public sealed class CollarSafeZoneEvaluationService(
    ICollarSafeZoneRepository safeZoneRepository,
    IPetRepository petRepository,
    INotificationRepository notificationRepository,
    IPushNotificationService pushNotificationService,
    ILogger<CollarSafeZoneEvaluationService> logger,
    IUserRepository? userRepository = null,
    IEmailSender? emailSender = null)
{
    public async Task EvaluateAsync(Collar collar, double lat, double lng, CancellationToken cancellationToken)
    {
        var zones = await safeZoneRepository.GetEnabledByCollarIdAsync(collar.Id, cancellationToken);
        if (zones.Count == 0) return;

        foreach (var zone in zones)
        {
            SafeZoneTransition transition;
            try
            {
                transition = zone.Evaluate(lat, lng);
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Skipping malformed safe zone {ZoneId} for collar {CollarId}", zone.Id, collar.Id);
                continue;
            }

            safeZoneRepository.Update(zone);

            if (transition == SafeZoneTransition.Breached)
                await NotifyBreachAsync(collar, zone, cancellationToken);
        }
    }

    private async Task NotifyBreachAsync(Collar collar, CollarSafeZone zone, CancellationToken cancellationToken)
    {
        var pet = await petRepository.GetByIdAsync(collar.PetId, cancellationToken);
        var petName = pet?.Name ?? "tu mascota";
        var title = "Salió de la zona segura";
        var body = $"{petName} salió de la zona \"{zone.Name}\".";

        var notification = Notification.Create(
            collar.OwnerId, NotificationType.CollarSafeZoneBreach, title, body, zone.Id.ToString());
        await notificationRepository.AddAsync(notification, cancellationToken);
        await pushNotificationService.SendAsync(collar.OwnerId, title, body, cancellationToken: cancellationToken);

        if (emailSender is not null && userRepository is not null)
        {
            var owner = await userRepository.GetByIdAsync(collar.OwnerId, cancellationToken);
            if (owner is not null && !string.IsNullOrWhiteSpace(owner.Email))
            {
                await emailSender.SendCollarSafeZoneBreachAsync(
                    owner.Email, owner.Name, petName, zone.Name, cancellationToken);
            }
        }
    }
}

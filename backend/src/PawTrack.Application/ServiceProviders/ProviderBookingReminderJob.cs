using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Notifications;

namespace PawTrack.Application.ServiceProviders;

public sealed class ProviderBookingReminderJob(
    IServiceProviderRepository providerRepository,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    ILogger<ProviderBookingReminderJob> logger)
{
    private static readonly TimeSpan ReminderWindow = TimeSpan.FromHours(24);
    private static readonly TimeSpan DeduplicationWindow = TimeSpan.FromHours(36);

    public async Task ExecuteAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var bookings = await providerRepository.GetConfirmedBookingsStartingBetweenAsync(
            now, now.Add(ReminderWindow), 500, ct);
        var notificationsCreated = 0;
        foreach (var booking in bookings)
        {
            var relatedEntityId = booking.Id.ToString();
            if (await notificationRepository.HasRecentByUserTypeAndEntityAsync(
                booking.CustomerUserId, NotificationType.ProviderBookingReminder,
                relatedEntityId, DeduplicationWindow, ct))
                continue;

            await notificationRepository.AddAsync(Notification.Create(
                booking.CustomerUserId,
                NotificationType.ProviderBookingReminder,
                "Recordatorio de reserva",
                $"Tu reserva de {booking.ServiceName} inicia {booking.StartsAt:dd/MM/yyyy HH:mm}.",
                relatedEntityId), ct);
            notificationsCreated++;
        }

        if (notificationsCreated == 0) return;
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Created {Count} provider booking reminders.", notificationsCreated);
    }
}
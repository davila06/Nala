using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Notifications;

namespace PawTrack.Application.ServiceProviders;

public sealed class ProviderVerificationRenewalReminderJob(
    IServiceProviderRepository repository,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    ILogger<ProviderVerificationRenewalReminderJob> logger)
{
    private static readonly TimeSpan ReminderCooldown = TimeSpan.FromDays(6);

    public async Task ExecuteAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var verifications = await repository.GetVerificationsExpiringWithinAsync(30, ct);
        var created = 0;
        foreach (var verification in verifications.Where(verification => verification.ExpiresAt >= today))
        {
            var provider = await repository.GetByIdAsync(verification.ServiceProviderId, ct);
            if (provider is null) continue;
            if (await notificationRepository.HasRecentByUserTypeAndEntityAsync(
                provider.UserId, NotificationType.SystemMessage, verification.Id.ToString(), ReminderCooldown, ct))
                continue;

            await notificationRepository.AddAsync(Notification.Create(
                provider.UserId,
                NotificationType.SystemMessage,
                "Verificacion por vencer",
                $"Tu verificacion vence el {verification.ExpiresAt:dd/MM/yyyy}. Carga una nueva evidencia antes de esa fecha.",
                verification.Id.ToString()), ct);
            created++;
        }

        if (created > 0) await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Provider verification renewal reminders created: {Count}.", created);
    }
}
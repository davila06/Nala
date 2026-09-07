using Microsoft.Extensions.Logging;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Notifications;

namespace PawTrack.Infrastructure.Certificates;

public sealed class VerificationRenewalReminderJob(
    IClinicVerificationRepository verificationRepository,
    IClinicVeterinarianRepository veterinarianRepository,
    IClinicRepository clinicRepository,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    ILogger<VerificationRenewalReminderJob> logger)
{
    private static readonly TimeSpan ReminderCooldown = TimeSpan.FromDays(6);

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var verifications = (await verificationRepository.GetExpiringWithinAsync(30, cancellationToken))
            .Where(v => v.ExpiresAt >= today)
            .ToList();
        var veterinarians = (await veterinarianRepository.GetExpiringWithinAsync(30, cancellationToken))
            .Where(v => v.ExpiresAt >= today)
            .ToList();

        var clinicIds = verifications.Select(v => v.ClinicId)
            .Concat(veterinarians.Select(v => v.ClinicId))
            .Distinct()
            .ToList();
        var clinics = (await clinicRepository.GetByIdsAsync(clinicIds, cancellationToken))
            .ToDictionary(clinic => clinic.Id);

        var created = 0;

        foreach (var verification in verifications)
        {
            if (!clinics.TryGetValue(verification.ClinicId, out var clinic)) continue;
            if (await notificationRepository.HasRecentByUserTypeAndEntityAsync(
                    clinic.UserId, NotificationType.SystemMessage, verification.Id.ToString(), ReminderCooldown, cancellationToken))
                continue;

            await notificationRepository.AddAsync(Notification.Create(
                clinic.UserId,
                NotificationType.SystemMessage,
                "Verificación de clínica por vencer",
                $"Tu verificación SENASA-ready vence el {verification.ExpiresAt:dd/MM/yyyy}. Solicita revalidación antes de esa fecha.",
                verification.Id.ToString()), cancellationToken);
            created++;
        }

        foreach (var veterinarian in veterinarians)
        {
            if (!clinics.TryGetValue(veterinarian.ClinicId, out var clinic)) continue;
            if (await notificationRepository.HasRecentByUserTypeAndEntityAsync(
                    clinic.UserId, NotificationType.SystemMessage, veterinarian.Id.ToString(), ReminderCooldown, cancellationToken))
                continue;

            await notificationRepository.AddAsync(Notification.Create(
                clinic.UserId,
                NotificationType.SystemMessage,
                "Veterinario autorizado por vencer",
                $"La autorización de {veterinarian.FullName} vence el {veterinarian.ExpiresAt:dd/MM/yyyy}.",
                veterinarian.Id.ToString()), cancellationToken);
            created++;
        }

        if (created > 0) await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("VerificationRenewalReminderJob finished. Notifications={Notifications}", created);
    }
}

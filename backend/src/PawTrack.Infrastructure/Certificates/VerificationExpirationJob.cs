using Microsoft.Extensions.Logging;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Notifications;

namespace PawTrack.Infrastructure.Certificates;

public sealed class VerificationExpirationJob(
    IClinicVerificationRepository verificationRepository,
    IClinicVeterinarianRepository veterinarianRepository,
    IVerificationAuditLogRepository auditLogRepository,
    IClinicRepository clinicRepository,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    ILogger<VerificationExpirationJob> logger)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var expiredVerifications = await verificationRepository.GetExpiringWithinAsync(0, cancellationToken);
        var expiredVeterinarians = await veterinarianRepository.GetExpiringWithinAsync(0, cancellationToken);
        var clinicIds = expiredVerifications.Select(v => v.ClinicId)
            .Concat(expiredVeterinarians.Select(v => v.ClinicId))
            .Distinct()
            .ToList();
        var clinics = (await clinicRepository.GetByIdsAsync(clinicIds, cancellationToken))
            .ToDictionary(clinic => clinic.Id);

        foreach (var verification in expiredVerifications)
        {
            verification.MarkExpired();
            verificationRepository.Update(verification);
            await auditLogRepository.AddAsync(VerificationAuditLog.Create(
                "ClinicVerification", verification.Id, VerificationAuditAction.ClinicVerificationExpired), cancellationToken);
            if (clinics.TryGetValue(verification.ClinicId, out var clinic))
            {
                await notificationRepository.AddAsync(Notification.Create(
                    clinic.UserId,
                    NotificationType.SystemMessage,
                    "Verificación de clínica vencida",
                    "Tu verificación SENASA-ready venció. Solicita revalidación y sube un documento actualizado para emitir nuevos pasaportes.",
                    verification.Id.ToString()), cancellationToken);
            }
        }

        foreach (var veterinarian in expiredVeterinarians)
        {
            veterinarian.MarkExpired();
            veterinarianRepository.Update(veterinarian);
            await auditLogRepository.AddAsync(VerificationAuditLog.Create(
                "ClinicVeterinarian", veterinarian.Id, VerificationAuditAction.VeterinarianExpired), cancellationToken);
            if (clinics.TryGetValue(veterinarian.ClinicId, out var clinic))
            {
                await notificationRepository.AddAsync(Notification.Create(
                    clinic.UserId,
                    NotificationType.SystemMessage,
                    "Veterinario autorizado vencido",
                    $"La autorización de {veterinarian.FullName} venció. Revalídala para continuar emitiendo pasaportes.",
                    veterinarian.Id.ToString()), cancellationToken);
            }
        }

        if (expiredVerifications.Count > 0 || expiredVeterinarians.Count > 0)
            await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "VerificationExpirationJob finished. ClinicVerifications={ClinicVerifications} Veterinarians={Veterinarians}",
            expiredVerifications.Count,
            expiredVeterinarians.Count);
    }
}

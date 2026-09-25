using MediatR;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Clinics.Commands.ManageClinicCrm;

public sealed record ClinicCommunicationTemplateDto(string Key, string Label, ClinicCommunicationPurpose Purpose, string WhatsAppTemplateName);

public static class ClinicCommunicationTemplates
{
    public static readonly IReadOnlyList<ClinicCommunicationTemplateDto> All =
    [
        new("appointment-confirmation", "Confirmacion de cita", ClinicCommunicationPurpose.AppointmentConfirmation, "clinic_appointment_confirmation"),
        new("clinical-follow-up", "Seguimiento clinico", ClinicCommunicationPurpose.ClinicalFollowUp, "clinic_clinical_follow_up"),
        new("vaccine-reminder", "Recordatorio de vacuna", ClinicCommunicationPurpose.VaccineReminder, "clinic_vaccine_reminder"),
        new("prescription-ready", "Receta disponible", ClinicCommunicationPurpose.PrescriptionDelivery, "clinic_prescription_ready"),
    ];

    public static string Render(string clinicName, string petName, ClinicCommunicationTemplateDto template) =>
        $"{clinicName}: {template.Label} para {petName}. Ingresa a NALA para consultar detalles o contacta directamente a la clinica.";
}

public sealed record SendClinicCommunicationTemplateCommand(Guid ClinicId, Guid ClinicUserId, Guid PetId, Guid RequestId, string TemplateKey, ClinicCommunicationChannel Channel)
    : IRequest<Result<Guid>>;

public sealed class SendClinicCommunicationTemplateCommandHandler(
    IClinicRepository clinics, IPetRepository pets, IUserRepository users, IClinicCrmRepository crm,
    IClinicEmailGateway email, IAuditLogRepository audit, IUnitOfWork unitOfWork)
    : IRequestHandler<SendClinicCommunicationTemplateCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(SendClinicCommunicationTemplateCommand request, CancellationToken ct)
    {
        if (request.RequestId == Guid.Empty) return Result.Failure<Guid>("Clave idempotente requerida.");
        var clinic = await clinics.GetByIdAsync(request.ClinicId, ct);
        if (clinic is null || clinic.UserId != request.ClinicUserId)
            return Result.Failure<Guid>("Acceso denegado.");
        var existing = await crm.GetActivityByRequestIdAsync(request.ClinicId, request.RequestId, ct);
        if (existing is not null)
            return existing.PetId == request.PetId && existing.Status == ClinicCommunicationStatus.Sent
                ? Result.Success(existing.Id)
                : Result.Failure<Guid>("Solicitud ya registrada; revisar estado antes de reintentar.");
        var template = ClinicCommunicationTemplates.All.FirstOrDefault(item => item.Key == request.TemplateKey);
        if (template is null) return Result.Failure<Guid>("Plantilla no autorizada.");
        if (request.Channel != ClinicCommunicationChannel.Email)
            return Result.Failure<Guid>("WhatsApp requiere plantilla y destino verificados por Meta; envio no habilitado.");
        var pet = await pets.GetByIdAsync(request.PetId, ct);
        if (pet is null || !await crm.HasClinicPatientRelationshipAsync(request.ClinicId, pet.Id, ct))
            return Result.Failure<Guid>("Paciente no vinculado a la clinica.");
        var preference = await crm.GetPreferenceAsync(request.ClinicId, pet.Id, request.Channel, template.Purpose, ct);
        if (preference?.IsOptedIn != true || preference.OwnerUserId != pet.OwnerId)
            return Result.Failure<Guid>("El tutor no autorizo este canal y proposito.");
        var owner = await users.GetByIdAsync(pet.OwnerId, ct);
        if (owner?.IsEmailVerified != true) return Result.Failure<Guid>("Correo del tutor no verificado.");

        var subject = template.Label;
        var activity = ClinicClientCommunicationActivity.Log(request.ClinicId, pet.Id, pet.OwnerId,
            request.Channel, template.Purpose, ClinicCommunicationDirection.Outbound, ClinicCommunicationStatus.Queued,
            subject, ClinicCommunicationTemplates.Render(clinic.Name, pet.Name, template), null, request.ClinicUserId, request.RequestId);
        await crm.AddActivityAsync(activity, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var result = await email.SendAsync(owner.Email, subject, activity.Body, request.RequestId, ct);
        if (result.IsFailure)
        {
            activity.MarkProviderFailed();
            await unitOfWork.SaveChangesAsync(ct);
            return Result.Failure<Guid>("El proveedor no confirmo la aceptacion del mensaje.");
        }
        activity.MarkProviderAccepted(result.Value!);
        await audit.AddAsync(AuditLogEntry.Create(request.ClinicUserId, AuditAction.ClinicCrmActivityLogged,
            "ClinicCommunicationActivity", activity.Id.ToString(), $"Email:{template.Key}:provider-accepted"), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(activity.Id);
    }
}

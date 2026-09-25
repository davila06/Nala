using FluentValidation;
using MediatR;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Common;
using PawTrack.Domain.Medical;

namespace PawTrack.Application.Clinics.Commands.CreateClinicalConsultation;

public sealed record ClinicalConsultationDto(
    Guid Id,
    Guid AppointmentId,
    Guid PetId,
    Guid VeterinarianId,
    string Status,
    string Reason,
    string Subjective,
    string Objective,
    string Assessment,
    string Plan,
    decimal? WeightKg,
    decimal? TemperatureC,
    int? HeartRateBpm,
    int? RespiratoryRateRpm,
    int? BodyConditionScore,
    int? PainScore,
    string? HydrationStatus,
    string Diagnosis,
    string Treatment,
    string OwnerSummary,
    string? PrescriptionInstructions,
    string? AttachmentUrl,
    string? SignedByName,
    DateTimeOffset? ClosedAt)
{
    public static ClinicalConsultationDto FromDomain(ClinicalConsultation consultation) => new(
        consultation.Id,
        consultation.AppointmentId,
        consultation.PetId,
        consultation.VeterinarianId,
        consultation.Status.ToString(),
        consultation.Reason,
        consultation.Subjective,
        consultation.Objective,
        consultation.Assessment,
        consultation.Plan,
        consultation.WeightKg,
        consultation.TemperatureC,
        consultation.HeartRateBpm,
        consultation.RespiratoryRateRpm,
        consultation.BodyConditionScore,
        consultation.PainScore,
        consultation.HydrationStatus,
        consultation.Diagnosis,
        consultation.Treatment,
        consultation.OwnerSummary,
        consultation.PrescriptionInstructions,
        consultation.AttachmentUrl,
        consultation.SignedByName,
        consultation.ClosedAt);
}

public sealed record CreateClinicalConsultationCommand(
    Guid ClinicId,
    Guid ClinicUserId,
    Guid AppointmentId,
    string Reason,
    string Subjective,
    string Objective,
    string Assessment,
    string Plan,
    decimal? WeightKg,
    decimal? TemperatureC,
    int? HeartRateBpm,
    int? RespiratoryRateRpm,
    int? BodyConditionScore,
    int? PainScore,
    string? HydrationStatus,
    string Diagnosis,
    string Treatment,
    string OwnerSummary,
    string? PrescriptionInstructions = null)
    : IRequest<Result<ClinicalConsultationDto>>;

public sealed class CreateClinicalConsultationCommandValidator
    : AbstractValidator<CreateClinicalConsultationCommand>
{
    public CreateClinicalConsultationCommandValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Subjective).MaximumLength(2000);
        RuleFor(x => x.Objective).MaximumLength(2000);
        RuleFor(x => x.Assessment).MaximumLength(2000);
        RuleFor(x => x.Plan).MaximumLength(2000);
        RuleFor(x => x.Diagnosis).MaximumLength(1000);
        RuleFor(x => x.Treatment).MaximumLength(1000);
        RuleFor(x => x.OwnerSummary).NotEmpty().MaximumLength(1500);
        RuleFor(x => x.PrescriptionInstructions).MaximumLength(1500);
        RuleFor(x => x.WeightKg).InclusiveBetween(0.01m, 250m).When(x => x.WeightKg.HasValue);
        RuleFor(x => x.TemperatureC).InclusiveBetween(30m, 45m).When(x => x.TemperatureC.HasValue);
        RuleFor(x => x.HeartRateBpm).InclusiveBetween(1, 400).When(x => x.HeartRateBpm.HasValue);
        RuleFor(x => x.RespiratoryRateRpm).InclusiveBetween(1, 200).When(x => x.RespiratoryRateRpm.HasValue);
        RuleFor(x => x.BodyConditionScore).InclusiveBetween(1, 9).When(x => x.BodyConditionScore.HasValue);
        RuleFor(x => x.PainScore).InclusiveBetween(0, 10).When(x => x.PainScore.HasValue);
        RuleFor(x => x.HydrationStatus).MaximumLength(100);
    }
}

public sealed class CreateClinicalConsultationCommandHandler(
    IClinicRepository clinicRepository,
    IVeterinarianAppointmentRepository appointmentRepository,
    IClinicVeterinarianRepository veterinarianRepository,
    IPetRepository petRepository,
    IClinicalConsultationRepository consultationRepository,
    IClinicMedicalAccessGrantRepository grantRepository,
    IClinicScanRepository clinicScanRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    IClinicStaffAccessRepository staffAccess)
    : IRequestHandler<CreateClinicalConsultationCommand, Result<ClinicalConsultationDto>>
{
    public async Task<Result<ClinicalConsultationDto>> Handle(CreateClinicalConsultationCommand request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.Status != ClinicStatus.Active)
            return Result.Failure<ClinicalConsultationDto>("La clínica no está activa.");

        var appointment = await appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
        if (appointment is null || appointment.ClinicId != request.ClinicId)
            return Result.Failure<ClinicalConsultationDto>("Cita no encontrada.");
        if (clinic.UserId != request.ClinicUserId)
        {
            var member = await staffAccess.GetAsync(request.ClinicId, request.ClinicUserId, cancellationToken);
            if (member?.Role != ClinicStaffRole.Veterinarian || !member.Allows(ClinicStaffPermission.WriteMedical)
                || member.VeterinarianId != appointment.VeterinarianId)
                return Result.Failure<ClinicalConsultationDto>("El veterinario asignado no tiene acceso de escritura.");
        }
        if (appointment.Status != VeterinarianAppointmentStatus.InConsultation)
            return Result.Failure<ClinicalConsultationDto>("La cita debe estar en consulta para documentarla.");

        if (await consultationRepository.GetByAppointmentIdAsync(appointment.Id, cancellationToken) is not null)
            return Result.Failure<ClinicalConsultationDto>("La cita ya tiene una consulta documentada.");

        var veterinarian = await veterinarianRepository.GetByIdAsync(appointment.VeterinarianId, cancellationToken);
        if (veterinarian is null || veterinarian.ClinicId != request.ClinicId || !veterinarian.IsActive)
            return Result.Failure<ClinicalConsultationDto>("El veterinario no está autorizado.");

        var pet = await petRepository.GetByIdAsync(appointment.PetId, cancellationToken);
        if (pet is null)
            return Result.Failure<ClinicalConsultationDto>("Mascota no encontrada.");

        var hasAccess =
            await clinicScanRepository.HasRecentScanAsync(request.ClinicId, pet.Id, 90, cancellationToken)
            || (await grantRepository.GetActiveGrantAsync(request.ClinicId, pet.Id, cancellationToken)) is { } grant
                && grant.HasPermission(ClinicMedicalAccessPermission.Write);
        if (!hasAccess)
            return Result.Failure<ClinicalConsultationDto>("La clínica no tiene acceso de escritura al expediente de esta mascota.");

        var consultation = ClinicalConsultation.Create(
            request.ClinicId,
            appointment.Id,
            pet.Id,
            veterinarian.Id,
            pet.OwnerId,
            request.ClinicUserId,
            request.Reason,
            request.Subjective,
            request.Objective,
            request.Assessment,
            request.Plan,
            request.WeightKg,
            request.TemperatureC,
            request.HeartRateBpm,
            request.RespiratoryRateRpm,
            request.BodyConditionScore,
            request.PainScore,
            request.HydrationStatus,
            request.Diagnosis,
            request.Treatment,
            request.OwnerSummary);
        consultation.SetPrescription(request.PrescriptionInstructions);

        await consultationRepository.AddAsync(consultation, cancellationToken);
        await auditLogRepository.AddAsync(
            AuditLogEntry.Create(
                request.ClinicUserId,
                AuditAction.ClinicalConsultationCreated,
                "ClinicalConsultation",
                consultation.Id.ToString(),
                appointment.Id.ToString()),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(ClinicalConsultationDto.FromDomain(consultation));
    }
}

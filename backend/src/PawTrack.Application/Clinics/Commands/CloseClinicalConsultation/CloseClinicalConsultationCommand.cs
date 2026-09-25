using FluentValidation;
using MediatR;
using PawTrack.Application.Clinics.Commands.CreateClinicalConsultation;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Common;
using PawTrack.Domain.Medical;

namespace PawTrack.Application.Clinics.Commands.CloseClinicalConsultation;

public sealed record CloseClinicalConsultationCommand(
    Guid ClinicId,
    Guid ClinicUserId,
    Guid ConsultationId,
    string SignedByName,
    IReadOnlyList<ClinicalInventoryUseInput>? InventoryUses = null)
    : IRequest<Result<ClinicalConsultationDto>>;

public sealed record ClinicalInventoryUseInput(
    Guid ItemId,
    int Quantity,
    ClinicInventoryMovementReason Reason);

public sealed class CloseClinicalConsultationCommandValidator
    : AbstractValidator<CloseClinicalConsultationCommand>
{
    public CloseClinicalConsultationCommandValidator()
    {
        RuleFor(x => x.ConsultationId).NotEmpty();
        RuleFor(x => x.SignedByName).NotEmpty().MaximumLength(160);
    }
}

public sealed class CloseClinicalConsultationCommandHandler(
    IClinicalConsultationRepository consultationRepository,
    IVeterinarianAppointmentRepository appointmentRepository,
    IMedicalRepository medicalRepository,
    IClinicInventoryRepository inventoryRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CloseClinicalConsultationCommand, Result<ClinicalConsultationDto>>
{
    public async Task<Result<ClinicalConsultationDto>> Handle(CloseClinicalConsultationCommand request, CancellationToken cancellationToken)
    {
        var consultation = await consultationRepository.GetByIdAsync(request.ConsultationId, cancellationToken);
        if (consultation is null || consultation.ClinicId != request.ClinicId)
            return Result.Failure<ClinicalConsultationDto>("Consulta no encontrada.");

        var appointment = await appointmentRepository.GetByIdAsync(consultation.AppointmentId, cancellationToken);
        if (appointment is null || appointment.ClinicId != request.ClinicId)
            return Result.Failure<ClinicalConsultationDto>("Cita no encontrada.");

        try
        {
            consultation.Close(request.ClinicUserId, request.SignedByName);
            if (appointment.Status == VeterinarianAppointmentStatus.InConsultation)
                appointment.Complete();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return Result.Failure<ClinicalConsultationDto>(ex.Message);
        }

        var record = MedicalRecord.Create(
            consultation.PetId,
            request.ClinicUserId,
            MedicalRecordType.Checkup,
            DateOnly.FromDateTime((consultation.ClosedAt ?? DateTimeOffset.UtcNow).UtcDateTime),
            BuildMedicalSummary(consultation),
            consultation.SignedByName,
            null,
            null,
            clinicId: consultation.ClinicId,
            weightKg: consultation.WeightKg);

        await medicalRepository.AddAsync(record, cancellationToken);
        var inventoryResult = await ConsumeInventoryAsync(request, consultation, cancellationToken);
        if (inventoryResult.IsFailure)
            return Result.Failure<ClinicalConsultationDto>(inventoryResult.Errors.ToArray());

        consultationRepository.Update(consultation);
        appointmentRepository.Update(appointment);
        await auditLogRepository.AddAsync(
            AuditLogEntry.Create(
                request.ClinicUserId,
                AuditAction.ClinicalConsultationClosed,
                "ClinicalConsultation",
                consultation.Id.ToString(),
                appointment.Id.ToString()),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(ClinicalConsultationDto.FromDomain(consultation));
    }

    private static string BuildMedicalSummary(Domain.Clinics.ClinicalConsultation consultation)
    {
        var prescription = string.IsNullOrWhiteSpace(consultation.PrescriptionInstructions)
            ? string.Empty
            : $"\nIndicaciones/receta: {consultation.PrescriptionInstructions}";
        return $"Motivo: {consultation.Reason}\nDiagnóstico: {consultation.Diagnosis}\nTratamiento: {consultation.Treatment}\nPlan: {consultation.Plan}\nResumen para dueño: {consultation.OwnerSummary}{prescription}";
    }

    private async Task<Result<bool>> ConsumeInventoryAsync(
        CloseClinicalConsultationCommand request,
        Domain.Clinics.ClinicalConsultation consultation,
        CancellationToken cancellationToken)
    {
        if (request.InventoryUses is null || request.InventoryUses.Count == 0)
            return Result.Success(true);

        foreach (var use in request.InventoryUses)
        {
            if (use.Quantity <= 0)
                return Result.Failure<bool>("La cantidad de inventario debe ser positiva.");

            var item = await inventoryRepository.GetItemByIdAsync(use.ItemId, cancellationToken);
            if (item is null || item.ClinicId != request.ClinicId)
                return Result.Failure<bool>("Producto clínico no encontrado.");

            var lots = await inventoryRepository.GetAvailableLotsByItemAsync(use.ItemId, cancellationToken);
            if (lots.Sum(lot => lot.AvailableQuantity) < use.Quantity)
                return Result.Failure<bool>("No hay stock suficiente.");

            var remaining = use.Quantity;
            foreach (var lot in lots)
            {
                if (remaining == 0) break;
                var consume = Math.Min(remaining, lot.AvailableQuantity);
                var movement = lot.Consume(
                    consume,
                    use.Reason,
                    request.ClinicUserId,
                    consultation.PetId,
                    consultation.Id,
                    certificateId: null);
                inventoryRepository.UpdateLot(lot);
                await inventoryRepository.AddMovementAsync(movement, cancellationToken);
                remaining -= consume;
            }
        }

        return Result.Success(true);
    }
}

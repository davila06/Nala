using MediatR;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Common;
using System.Text;

namespace PawTrack.Application.Clinics.Queries.DownloadClinicalConsultationPrescription;

public sealed record DownloadClinicalConsultationPrescriptionQuery(
    Guid ClinicId,
    Guid RequestingUserId,
    Guid ConsultationId)
    : IRequest<Result<byte[]>>;

public sealed class DownloadClinicalConsultationPrescriptionQueryHandler(
    IClinicRepository clinicRepository,
    IClinicalConsultationRepository consultationRepository)
    : IRequestHandler<DownloadClinicalConsultationPrescriptionQuery, Result<byte[]>>
{
    public async Task<Result<byte[]>> Handle(
        DownloadClinicalConsultationPrescriptionQuery request,
        CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.RequestingUserId)
            return Result.Failure<byte[]>("Acceso denegado.");

        var consultation = await consultationRepository.GetByIdAsync(request.ConsultationId, cancellationToken);
        if (consultation is null || consultation.ClinicId != request.ClinicId)
            return Result.Failure<byte[]>("Consulta no encontrada.");
        if (consultation.Status != ClinicalConsultationStatus.Closed)
            return Result.Failure<byte[]>("La consulta debe estar cerrada para imprimir indicaciones.");

        var text = new StringBuilder()
            .AppendLine("NALA - Indicaciones veterinarias")
            .AppendLine($"Fecha: {consultation.ClosedAt:dd/MM/yyyy HH:mm}")
            .AppendLine($"Firma: {consultation.SignedByName}")
            .AppendLine()
            .AppendLine($"Motivo: {consultation.Reason}")
            .AppendLine($"Diagnóstico: {consultation.Diagnosis}")
            .AppendLine($"Tratamiento: {consultation.Treatment}")
            .AppendLine($"Plan: {consultation.Plan}")
            .AppendLine($"Resumen: {consultation.OwnerSummary}")
            .AppendLine();
        if (!string.IsNullOrWhiteSpace(consultation.PrescriptionInstructions))
            text.AppendLine("Receta / indicaciones:").AppendLine(consultation.PrescriptionInstructions);

        return Result.Success(Encoding.UTF8.GetBytes(text.ToString()));
    }
}

using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Medical;
using PawTrack.Domain.Medical;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Clinics.Commands.ExportClinicMedical;

public sealed record ExportClinicMedicalCommand(Guid ClinicId, Guid RequestingUserId, Guid PetId)
    : IRequest<Result<ClinicMedicalExportDto>>;

public sealed record ClinicMedicalExportDto(
    Guid ExportId, Guid PetId, int RecordCount, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);

public sealed class ExportClinicMedicalCommandHandler(
    IClinicRepository clinicRepository,
    IPetRepository petRepository,
    IClinicMedicalAccessGrantRepository grantRepository,
    IMedicalRepository medicalRepository,
    IMedicalPdfExporter pdfExporter,
    IClinicMedicalExportRepository exportRepository,
    IBlobStorageService blobStorage,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ExportClinicMedicalCommand, Result<ClinicMedicalExportDto>>
{
    private const int MonthlyExportLimit = 20;
    private static readonly TimeSpan ExportLifetime = TimeSpan.FromHours(24);

    public async Task<Result<ClinicMedicalExportDto>> Handle(ExportClinicMedicalCommand request, CancellationToken ct)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, ct);
        if (clinic is null || clinic.UserId != request.RequestingUserId || clinic.Status != Domain.Clinics.ClinicStatus.Active)
            return Result.Failure<ClinicMedicalExportDto>("Acceso denegado.");

        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null) return Result.Failure<ClinicMedicalExportDto>("Mascota no encontrada.");
        var grant = await grantRepository.GetActiveGrantAsync(request.ClinicId, request.PetId, ct);
        if (grant is null || !grant.HasPermission(ClinicMedicalAccessPermission.Export))
            return Result.Failure<ClinicMedicalExportDto>("El consentimiento no permite exportar el expediente.");

        var monthStart = new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        if (await exportRepository.CountForClinicSinceAsync(request.ClinicId, monthStart, ct) >= MonthlyExportLimit)
            return Result.Failure<ClinicMedicalExportDto>("La clínica alcanzó el límite mensual de exportaciones.");
        if (await exportRepository.ExistsForPetSinceAsync(request.ClinicId, request.PetId, DateTimeOffset.UtcNow.AddHours(-24), ct))
            return Result.Failure<ClinicMedicalExportDto>("Esta mascota ya fue exportada durante las últimas 24 horas.");

        var records = (await medicalRepository.GetByPetIdAsync(request.PetId, ct)).Select(MedicalRecordDto.FromDomain).ToList();
        var reminders = (await medicalRepository.GetUpcomingRemindersAsync(request.PetId, ct)).Select(VetReminderDto.FromDomain).ToList();
        var bytes = await pdfExporter.ExportAsync(pet.Name, records, reminders, ct);
        var exportId = Guid.CreateVersion7();
        var blobName = $"{clinic.Id}/{pet.Id}/{exportId}.pdf";
        using var stream = new MemoryStream(bytes);
        var blobUrl = await blobStorage.UploadAsync("clinic-medical-exports", blobName, stream, "application/pdf", ct);
        var export = ClinicMedicalExport.Complete(clinic.Id, pet.Id, request.RequestingUserId, blobUrl, records.Count, ExportLifetime);
        await exportRepository.AddAsync(export, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(new ClinicMedicalExportDto(export.Id, export.PetId, export.RecordCount, export.CreatedAt, export.ExpiresAt));
    }
}
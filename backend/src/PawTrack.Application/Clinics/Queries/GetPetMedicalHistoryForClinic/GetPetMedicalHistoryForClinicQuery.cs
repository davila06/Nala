using System.Text.RegularExpressions;
using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Medical;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Common;
using PawTrack.Domain.Medical;
namespace PawTrack.Application.Clinics.Queries.GetPetMedicalHistoryForClinic;

// ── Query ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Returns a pet's full medical history only with an active owner-approved
/// grant that includes the Read permission. A QR/chip scan identifies the pet
/// and is recorded as contact history, but is not consent to disclose records.
/// </summary>
public sealed record GetPetMedicalHistoryForClinicQuery(
    Guid ClinicId,
    Guid? PetId,
    string? QrOrChipInput,
    ScanInputType? InputType,
    Guid ClinicUserId)
    : IRequest<Result<ClinicPatientHistoryDto>>;

public sealed record ClinicPatientHistoryDto(
    Guid PetId,
    string PetName,
    string Species,
    string? Breed,
    string? PhotoUrl,
    DateTimeOffset? LastSeenAt,
    IReadOnlyList<MedicalRecordDto> Records);

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class GetPetMedicalHistoryForClinicQueryHandler(
    IClinicRepository clinicRepository,
    IClinicScanRepository clinicScanRepository,
    IClinicMedicalAccessGrantRepository grantRepository,
    IPetRepository petRepository,
    IMedicalRepository medicalRepository,
    IClinicMedicalAccessLogRepository accessLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GetPetMedicalHistoryForClinicQuery, Result<ClinicPatientHistoryDto>>
{
    private static readonly Regex PetIdFromQrPattern =
        new(@"\/p\/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public async Task<Result<ClinicPatientHistoryDto>> Handle(
        GetPetMedicalHistoryForClinicQuery request, CancellationToken ct)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, ct);
        if (clinic is null || clinic.Status != ClinicStatus.Active || clinic.UserId != request.ClinicUserId)
            return Result.Failure<ClinicPatientHistoryDto>("La clínica no está activa.");

        var (pet, inlineScan) = await ResolvePetAsync(request, ct);
        if (pet is null)
            return Result.Failure<ClinicPatientHistoryDto>(
                "No se pudo identificar la mascota. Verifique el QR o chip.");

        var grant = await grantRepository.GetActiveGrantAsync(request.ClinicId, pet.Id, ct);
        var hasAccess = grant is not null && grant.HasPermission(ClinicMedicalAccessPermission.Read);
        var accessMethod = hasAccess
            ? "active_grant"
            : inlineScan is not null ? "inline_scan_without_read_grant" : "no_active_read_grant";
        var accessReason = hasAccess
            ? "Active owner consent grant with read permission."
            : "An active owner-approved read grant is required; a scan alone does not disclose medical history.";

        if (!hasAccess)
        {
            await RecordAccessAsync(pet.Id, request, accessMethod, "denied", accessReason, ct);
            return Result.Failure<ClinicPatientHistoryDto>(
                "La clínica requiere un permiso de lectura activo, aprobado por el dueño. Escanear el QR no concede acceso al expediente.");
        }

        var records = await medicalRepository.GetByPetIdAsync(pet.Id, ct);
        var lastScan = await clinicScanRepository.GetLastScanDateAsync(request.ClinicId, pet.Id, ct);

        if (inlineScan is not null)
            await clinicScanRepository.AddAsync(inlineScan, ct);

        // Audit: record this access (fire-and-persist — must not fail the query if log write fails)
        await RecordAccessAsync(pet.Id, request, accessMethod, "allowed", accessReason, ct);

        return Result.Success(new ClinicPatientHistoryDto(
            pet.Id,
            pet.Name,
            pet.Species.ToString(),
            pet.Breed,
            pet.PhotoUrl,
            lastScan,
            records.Select(MedicalRecordDto.FromDomain).ToList()));
    }

    private async Task RecordAccessAsync(
        Guid petId,
        GetPetMedicalHistoryForClinicQuery request,
        string accessMethod,
        string outcome,
        string reason,
        CancellationToken ct)
    {
        try
        {
            var log = ClinicMedicalAccessLog.Create(
                petId,
                request.ClinicId,
                request.ClinicUserId,
                operation: "read_medical_history",
                permission: ClinicMedicalAccessPermission.Read,
                accessMethod,
                outcome,
                reason);
            await accessLogRepository.AddAsync(log, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch
        {
            // Access decisions remain available if audit persistence is temporarily unavailable.
        }
    }

    private async Task<(Domain.Pets.Pet? Pet, ClinicScan? InlineScan)> ResolvePetAsync(
        GetPetMedicalHistoryForClinicQuery request, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.QrOrChipInput))
        {
            var inputType = request.InputType ?? ScanInputType.Qr;
            Domain.Pets.Pet? resolvedPet = null;

            if (inputType == ScanInputType.Qr)
            {
                var m = PetIdFromQrPattern.Match(request.QrOrChipInput);
                if (m.Success && Guid.TryParse(m.Groups[1].Value, out var petId))
                    resolvedPet = await petRepository.GetByIdAsync(petId, ct);
            }
            else if (inputType == ScanInputType.RfidChip)
            {
                resolvedPet = await petRepository.GetByMicrochipIdAsync(
                    request.QrOrChipInput.Trim().ToUpperInvariant(), ct);
            }

            return resolvedPet is null
                ? (null, null)
                : (resolvedPet, ClinicScan.Create(request.ClinicId, request.QrOrChipInput, inputType, resolvedPet.Id));
        }

        if (!request.PetId.HasValue) return (null, null);

        return (await petRepository.GetByIdAsync(request.PetId.Value, ct), null);
    }
}

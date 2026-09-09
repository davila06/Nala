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
/// Returns a pet's full medical history to an authenticated clinic.
/// Access gate (A+B): clinic must have a recent scan for the pet (Option A)
/// or provide the QR/chip from the current consult (Option B).
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
    private const int RecentScanWindowDays = 90;
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

        // Access gate: resolve the first concrete authorization path so the audit
        // record explains why the medical history was disclosed.
        var accessMethod = "unknown";
        var accessReason = "No valid clinic access path was found.";
        var hasAccess = inlineScan is not null;
        if (hasAccess)
        {
            accessMethod = "inline_scan";
            accessReason = "Current consultation QR/chip scan.";
        }
        else if (await clinicScanRepository.HasRecentScanAsync(request.ClinicId, pet.Id, RecentScanWindowDays, ct))
        {
            hasAccess = true;
            accessMethod = "recent_scan";
            accessReason = $"Clinic scan within {RecentScanWindowDays} days.";
        }
        else if (await grantRepository.GetActiveGrantAsync(request.ClinicId, pet.Id, ct)
            is { } grant && grant.HasPermission(ClinicMedicalAccessPermission.Read))
        {
            hasAccess = true;
            accessMethod = "active_grant";
            accessReason = "Active owner consent grant with read permission.";
        }

        if (!hasAccess)
        {
            await RecordAccessAsync(pet.Id, request, accessMethod, "denied", accessReason, ct);
            return Result.Failure<ClinicPatientHistoryDto>(
                "La clínica no tiene acceso a esta mascota. Escanee el QR o solicite acceso permanente al dueño.");
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

using System.Text.RegularExpressions;
using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Medical;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Common;
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
    ScanInputType? InputType)
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
    IMedicalRepository medicalRepository)
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
        if (clinic is null || clinic.Status != ClinicStatus.Active)
            return Result.Failure<ClinicPatientHistoryDto>("La clínica no está activa.");

        var pet = await ResolvePetAsync(request, ct);
        if (pet is null)
            return Result.Failure<ClinicPatientHistoryDto>(
                "No se pudo identificar la mascota. Verifique el QR o chip.");

        // Access gate: Option B inline → granted; Option A scan history → granted; Option C active grant → granted
        var hasAccess = request.QrOrChipInput is not null
            || await clinicScanRepository.HasRecentScanAsync(request.ClinicId, pet.Id, RecentScanWindowDays, ct)
            || await grantRepository.HasActiveGrantAsync(request.ClinicId, pet.Id, ct);

        if (!hasAccess)
            return Result.Failure<ClinicPatientHistoryDto>(
                "La clínica no tiene acceso a esta mascota. Escanee el QR o solicite acceso permanente al dueño.");

        var records = await medicalRepository.GetByPetIdAsync(pet.Id, ct);
        var lastScan = await clinicScanRepository.GetLastScanDateAsync(request.ClinicId, pet.Id, ct);

        return Result.Success(new ClinicPatientHistoryDto(
            pet.Id,
            pet.Name,
            pet.Species.ToString(),
            pet.Breed,
            pet.PhotoUrl,
            lastScan,
            records.Select(MedicalRecordDto.FromDomain).ToList()));
    }

    private async Task<Domain.Pets.Pet?> ResolvePetAsync(
        GetPetMedicalHistoryForClinicQuery request, CancellationToken ct)
    {
        if (request.PetId.HasValue)
            return await petRepository.GetByIdAsync(request.PetId.Value, ct);

        if (string.IsNullOrWhiteSpace(request.QrOrChipInput)) return null;

        var inputType = request.InputType ?? ScanInputType.Qr;
        if (inputType == ScanInputType.Qr)
        {
            var m = PetIdFromQrPattern.Match(request.QrOrChipInput);
            if (m.Success && Guid.TryParse(m.Groups[1].Value, out var petId))
                return await petRepository.GetByIdAsync(petId, ct);
        }
        else if (inputType == ScanInputType.RfidChip)
        {
            return await petRepository.GetByMicrochipIdAsync(
                request.QrOrChipInput.Trim().ToUpperInvariant(), ct);
        }
        return null;
    }
}

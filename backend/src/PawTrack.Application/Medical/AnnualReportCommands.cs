using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Common;
using PawTrack.Domain.Medical;

namespace PawTrack.Application.Medical;

// ── DTO passed to the PDF service ────────────────────────────────────────────

public sealed record AnnualReportData(
    // Pet
    string PetName,
    string Species,
    string? Breed,
    int? AgeMonths,
    string? PhotoUrl,
    int Year,
    // Health summary
    IReadOnlyList<AnnualVetVisitDto> VetVisits,
    AnnualWeightDto? WeightSummary,
    // Activity
    int TotalQrScans,
    // Lost events
    IReadOnlyList<AnnualLostEventDto> LostEvents,
    // Reminders completed
    int RemindersCompleted);

public sealed record AnnualVetVisitDto(DateOnly Date, string Type, string Description, string? ClinicName);
public sealed record AnnualWeightDto(decimal FirstKg, decimal LastKg, DateOnly FirstDate, DateOnly LastDate);
public sealed record AnnualLostEventDto(DateOnly ReportedDate, DateOnly? ResolvedDate, bool Reunited, int? DaysLost);

// ── Interface (implemented in Infrastructure) ─────────────────────────────────

public interface IAnnualReportPdfGenerator
{
    Task<byte[]> GenerateAsync(AnnualReportData data, CancellationToken ct = default);
}

// ── Query ─────────────────────────────────────────────────────────────────────

public sealed record GenerateAnnualReportQuery(Guid PetId, Guid RequestingUserId, int Year)
    : IRequest<Result<byte[]>>;

public sealed class GenerateAnnualReportQueryHandler(
    IPetRepository petRepository,
    IMedicalRepository medicalRepository,
    ILostPetRepository lostPetRepository,
    IQrScanEventRepository qrScanRepository,
    IFamilyRepository familyRepository,
    ISubscriptionService subscriptionService,
    IAnnualReportPdfGenerator pdfGenerator)
    : IRequestHandler<GenerateAnnualReportQuery, Result<byte[]>>
{
    public async Task<Result<byte[]>> Handle(GenerateAnnualReportQuery request, CancellationToken ct)
    {
        var isFamilia = await subscriptionService.IsFamiliaAsync(request.RequestingUserId, ct);
        if (!isFamilia)
            return Result.Failure<byte[]>("El informe anual requiere el plan Familia.");

        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null) return Result.Failure<byte[]>("Mascota no encontrada.");

        var canAccess = pet.OwnerId == request.RequestingUserId
            || (await familyRepository.GetActiveMemberIdsAsync(pet.OwnerId, ct)).Contains(request.RequestingUserId);
        if (!canAccess) return Result.Failure<byte[]>("Acceso denegado.");

        var yearStart = new DateOnly(request.Year, 1, 1);
        var yearEnd = new DateOnly(request.Year, 12, 31);

        // Medical records in the year
        var allRecords = await medicalRepository.GetByPetIdAsync(request.PetId, ct);
        var yearRecords = allRecords
            .Where(r => r.Date >= yearStart && r.Date <= yearEnd)
            .OrderBy(r => r.Date)
            .ToList();

        var vetVisits = yearRecords
            .Select(r => new AnnualVetVisitDto(r.Date, r.Type.ToString(), r.Description, r.ClinicName))
            .ToList()
            .AsReadOnly();

        // Weight trend
        var weightEntries = yearRecords.Where(r => r.WeightKg.HasValue).ToList();
        AnnualWeightDto? weightSummary = weightEntries.Count >= 2
            ? new AnnualWeightDto(
                weightEntries[0].WeightKg!.Value, weightEntries[^1].WeightKg!.Value,
                weightEntries[0].Date, weightEntries[^1].Date)
            : null;

        // QR scans for the year (load recent batch, filter in-memory)
        var scans = await qrScanRepository.GetByPetIdAsync(request.PetId, 1000, ct);
        var yearScanCount = scans.Count(s =>
            s.ScannedAt.Year == request.Year);

        // Lost events in the year
        var allLostEvents = await lostPetRepository.GetAllByPetIdAsync(request.PetId, ct);
        var lostEvents = allLostEvents
            .Where(e => e.ReportedAt.Year == request.Year || (e.ResolvedAt.HasValue && e.ResolvedAt.Value.Year == request.Year))
            .Select(e => new AnnualLostEventDto(
                DateOnly.FromDateTime(e.ReportedAt.DateTime),
                e.ResolvedAt.HasValue ? DateOnly.FromDateTime(e.ResolvedAt.Value.DateTime) : null,
                e.Status == PawTrack.Domain.LostPets.LostPetStatus.Reunited,
                e.ResolvedAt.HasValue
                    ? (int)(e.ResolvedAt.Value - e.ReportedAt).TotalDays
                    : null))
            .ToList()
            .AsReadOnly();

        // Reminders completed in the year
        var reminders = await medicalRepository.GetUpcomingRemindersAsync(request.PetId, ct);
        var completedInYear = allRecords
            .Count(r => r.Date >= yearStart && r.Date <= yearEnd && r.Type == MedicalRecordType.Checkup);

        // Pet age in months
        int? ageMonths = null;
        if (pet.BirthDate.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            ageMonths = (today.Year - pet.BirthDate.Value.Year) * 12
                      + today.Month - pet.BirthDate.Value.Month;
        }

        var data = new AnnualReportData(
            pet.Name, pet.Species.ToString(), pet.Breed, ageMonths, pet.PhotoUrl,
            request.Year, vetVisits, weightSummary, yearScanCount, lostEvents, completedInYear);

        var pdf = await pdfGenerator.GenerateAsync(data, ct);
        return Result.Success(pdf);
    }
}

using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Regulatory.Dtos;
using PawTrack.Application.Regulatory.Interfaces;
using PawTrack.Domain.Adoptions;
using PawTrack.Domain.AnimalWelfare;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.LostPets;
using PawTrack.Domain.Municipalities;
using PawTrack.Domain.Pets;
using PawTrack.Domain.Regulatory;
using PawTrack.Infrastructure.Persistence;
using PawTrack.Application.Regulatory.Services;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace PawTrack.Infrastructure.Regulatory;

public sealed class RegulatoryReportQueryService(
    PawTrackDbContext dbContext,
    IDistributedCache cache) : IRegulatoryReportQueryService
{
    public async Task<NalaOverviewDto> GetOverviewAsync(
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"regulatory:overview:{periodStart:yyyyMMdd}:{periodEnd:yyyyMMdd}";
        var cached = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached is not null)
            return JsonSerializer.Deserialize<NalaOverviewDto>(cached)!;

        var (start, end) = ToUtcRange(periodStart, periodEnd);
        var activeLostPets = await dbContext.LostPetEvents.AsNoTracking()
            .CountAsync(e => e.Status == LostPetStatus.Active, cancellationToken);
        var reunitedPets = await dbContext.LostPetEvents.AsNoTracking()
            .CountAsync(e => e.Status == LostPetStatus.Reunited && e.ResolvedAt >= start && e.ResolvedAt < end, cancellationToken);
        var capturedAnimals = await dbContext.CapturedAnimals.AsNoTracking()
            .CountAsync(e => e.CapturedAt >= start && e.CapturedAt < end, cancellationToken);
        var availableAdoptions = await dbContext.AdoptableAnimals.AsNoTracking()
            .CountAsync(e => e.Status == AdoptionStatus.Available, cancellationToken);
        var adoptedAnimals = await dbContext.AdoptableAnimals.AsNoTracking()
            .CountAsync(e => e.Status == AdoptionStatus.Adopted && e.AdoptedAt >= start && e.AdoptedAt < end, cancellationToken);
        var openWelfareCases = await dbContext.AnimalWelfareCases.AsNoTracking()
            .CountAsync(e => e.Status != WelfareCaseStatus.Resolved
                && e.Status != WelfareCaseStatus.Dismissed
                && e.Status != WelfareCaseStatus.ClosedNoAction, cancellationToken);
        var verifiedMicrochips = await dbContext.Pets.AsNoTracking()
            .CountAsync(e => e.MicrochipVerificationStatus == MicrochipVerificationStatus.Verified, cancellationToken);
        var validCertificates = await dbContext.VetCertificates.AsNoTracking()
            .CountAsync(e => !e.IsRevoked && (e.ValidUntil == null || e.ValidUntil > DateTimeOffset.UtcNow), cancellationToken);

        var result = new NalaOverviewDto(
            periodStart,
            periodEnd,
            activeLostPets,
            reunitedPets,
            capturedAnimals,
            availableAdoptions,
            adoptedAnimals,
            openWelfareCases,
            verifiedMicrochips,
            validCertificates,
            DateTimeOffset.UtcNow,
            IsSuppressed: false);
        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2),
        }, cancellationToken);
        return result;
    }

    public async Task<RegulatoryReportData> GetReportDataAsync(
        ReportType reportType,
        DateOnly periodStart,
        DateOnly periodEnd,
        RegulatoryReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var (start, end) = ToUtcRange(periodStart, periodEnd);
        var threshold = 5;
        List<RegulatoryReportRow> rows;

        switch (reportType)
        {
            case ReportType.NalaOverview:
                var overview = await GetOverviewAsync(periodStart, periodEnd, cancellationToken);
                rows = OverviewRows(overview);
                return new RegulatoryReportData(reportType, periodStart, periodEnd, rows, false, overview);

            case ReportType.Recovery:
                var recovery = await dbContext.LostPetEvents.AsNoTracking()
                    .Where(e => e.ReportedAt >= start && e.ReportedAt < end
                        && (string.IsNullOrWhiteSpace(filter.Canton) || e.CantonName == filter.Canton))
                    .GroupBy(e => e.Status)
                    .Select(group => new { Category = group.Key.ToString(), Count = group.Count() })
                    .ToListAsync(cancellationToken);
                rows = ApplyThreshold(recovery.Select(item => (item.Category, "Recovery", item.Count)), threshold);
                break;

            case ReportType.MunicipalCaptures:
                var captures = await dbContext.CapturedAnimals.AsNoTracking()
                    .Where(e => e.CapturedAt >= start && e.CapturedAt < end
                        && (string.IsNullOrWhiteSpace(filter.Canton) || e.Canton == filter.Canton)
                        && (string.IsNullOrWhiteSpace(filter.Species) || e.Species == filter.Species))
                    .GroupBy(e => e.Status)
                    .Select(group => new { Category = group.Key.ToString(), Count = group.Count() })
                    .ToListAsync(cancellationToken);
                rows = ApplyThreshold(captures.Select(item => (item.Category, "MunicipalCapture", item.Count)), threshold);
                break;

            case ReportType.Adoptions:
                var adoptions = await dbContext.AdoptableAnimals.AsNoTracking()
                    .Where(e => e.PublishedAt >= start && e.PublishedAt < end
                        && (string.IsNullOrWhiteSpace(filter.Species) || e.Species.ToString() == filter.Species))
                    .GroupBy(e => e.Status)
                    .Select(group => new { Category = group.Key.ToString(), Count = group.Count() })
                    .ToListAsync(cancellationToken);
                rows = ApplyThreshold(adoptions.Select(item => (item.Category, "Adoption", item.Count)), threshold);
                break;

            case ReportType.WelfareCases:
                var welfare = await dbContext.AnimalWelfareCases.AsNoTracking()
                    .Where(e => e.CreatedAt >= start && e.CreatedAt < end
                        && (string.IsNullOrWhiteSpace(filter.Canton) || e.Canton == filter.Canton)
                        && (string.IsNullOrWhiteSpace(filter.Status) || e.Status.ToString() == filter.Status))
                    .GroupBy(e => e.Status)
                    .Select(group => new { Category = group.Key.ToString(), Count = group.Count() })
                    .ToListAsync(cancellationToken);
                rows = ApplyThreshold(welfare.Select(item => (item.Category, "WelfareCase", item.Count)), threshold);
                break;

            case ReportType.SanitaryIdentity:
                var sanitary = await dbContext.Pets.AsNoTracking()
                    .GroupBy(e => e.MicrochipVerificationStatus)
                    .Select(group => new { Category = group.Key.ToString(), Count = group.Count() })
                    .ToListAsync(cancellationToken);
                rows = ApplyThreshold(sanitary.Select(item => (item.Category, "SanitaryIdentity", item.Count)), threshold);
                break;

            case ReportType.NetworkCoverage:
                var verifiedClinics = await dbContext.ClinicVerifications.AsNoTracking().CountAsync(e => e.IsActive, cancellationToken);
                var activeMunicipalities = await dbContext.MunicipalityProfiles.AsNoTracking().CountAsync(e => e.IsActive, cancellationToken);
                rows = ApplyThreshold(
                    [("VerifiedClinics", "Network", verifiedClinics), ("ActiveMunicipalities", "Network", activeMunicipalities)],
                    threshold);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(reportType), reportType, "Unsupported report type.");
        }

        return new RegulatoryReportData(reportType, periodStart, periodEnd, rows, rows.Any(row => row.IsSuppressed), null);
    }

    public async Task<PublicImpactStatsDto> GetPublicImpactStatsAsync(
        DateOnly periodStart,
        DateOnly periodEnd,
        int suppressionThreshold,
        CancellationToken cancellationToken = default)
    {
        var (start, end) = ToUtcRange(periodStart, periodEnd);
        var reunited = await dbContext.LostPetEvents.AsNoTracking()
            .CountAsync(e => e.Status == LostPetStatus.Reunited && e.ResolvedAt >= start && e.ResolvedAt < end, cancellationToken);
        var adopted = await dbContext.AdoptableAnimals.AsNoTracking()
            .CountAsync(e => e.Status == AdoptionStatus.Adopted && e.AdoptedAt >= start && e.AdoptedAt < end, cancellationToken);
        var resolvedWelfare = await dbContext.AnimalWelfareCases.AsNoTracking()
            .CountAsync(e => (e.Status == WelfareCaseStatus.Resolved || e.Status == WelfareCaseStatus.ClosedNoAction)
                && e.ClosedAt >= start && e.ClosedAt < end, cancellationToken);
        var total = reunited + adopted + resolvedWelfare;
        var suppressed = total < suppressionThreshold;

        return suppressed
            ? new PublicImpactStatsDto(periodStart, periodEnd, 0, 0, 0, true, DateTimeOffset.UtcNow)
            : new PublicImpactStatsDto(periodStart, periodEnd, reunited, adopted, resolvedWelfare, false, DateTimeOffset.UtcNow);
    }

    public async Task<IReadOnlyList<NalaMapCellDto>> GetMapCellsAsync(
        double south,
        double north,
        double west,
        double east,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"regulatory:map:{south:F3}:{north:F3}:{west:F3}:{east:F3}";
        var cached = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached is not null)
            return JsonSerializer.Deserialize<IReadOnlyList<NalaMapCellDto>>(cached)!;

        var lostPoints = await dbContext.LostPetEvents.AsNoTracking()
            .Where(e => e.Status == LostPetStatus.Active
                && e.LastSeenLat >= south && e.LastSeenLat <= north
                && e.LastSeenLng >= west && e.LastSeenLng <= east)
            .Select(e => new MapPoint(e.LastSeenLat!.Value, e.LastSeenLng!.Value, "Lost", e.CantonName ?? "Unknown"))
            .Take(500)
            .ToListAsync(cancellationToken);
        var welfarePoints = await dbContext.AnimalWelfareCases.AsNoTracking()
            .Where(e => e.Status != WelfareCaseStatus.Resolved
                && e.Status != WelfareCaseStatus.Dismissed
                && e.Status != WelfareCaseStatus.ClosedNoAction
                && e.ApproxLat >= south && e.ApproxLat <= north
                && e.ApproxLng >= west && e.ApproxLng <= east)
            .Select(e => new MapPoint(e.ApproxLat!.Value, e.ApproxLng!.Value, "Welfare", e.Canton))
            .Take(500)
            .ToListAsync(cancellationToken);
        var clinicPoints = await dbContext.Clinics.AsNoTracking()
            .Where(e => e.Status == Domain.Clinics.ClinicStatus.Active
                && (double)e.Lat >= south && (double)e.Lat <= north
                && (double)e.Lng >= west && (double)e.Lng <= east)
            .Select(e => new MapPoint((double)e.Lat, (double)e.Lng, "Clinic", "Unknown"))
            .Take(500)
            .ToListAsync(cancellationToken);

        var result = NalaMapGridService.Aggregate(
                lostPoints.Concat(welfarePoints).Concat(clinicPoints),
                south, north, west, east, threshold: 5)
            .Select(cell => new NalaMapCellDto(
                cell.Latitude, cell.Longitude, cell.Layer, cell.Canton, cell.Count, cell.IsSuppressed))
            .ToList();
        await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1),
        }, cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<NalaTrendPointDto>> GetTrendsAsync(
        DateOnly periodStart,
        DateOnly periodEnd,
        string? canton,
        CancellationToken cancellationToken = default)
    {
        var (start, end) = ToUtcRange(periodStart, periodEnd);
        var lost = await dbContext.LostPetEvents.AsNoTracking()
            .Where(e => e.ReportedAt >= start && e.ReportedAt < end
                && (string.IsNullOrWhiteSpace(canton) || e.CantonName == canton))
            .GroupBy(e => e.ReportedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var reunited = await dbContext.LostPetEvents.AsNoTracking()
            .Where(e => e.ResolvedAt >= start && e.ResolvedAt < end && e.Status == LostPetStatus.Reunited
                && (string.IsNullOrWhiteSpace(canton) || e.CantonName == canton))
            .GroupBy(e => e.ResolvedAt!.Value.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var captures = await dbContext.CapturedAnimals.AsNoTracking()
            .Where(e => e.CapturedAt >= start && e.CapturedAt < end
                && (string.IsNullOrWhiteSpace(canton) || e.Canton == canton))
            .GroupBy(e => e.CapturedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var adopted = await dbContext.AdoptableAnimals.AsNoTracking()
            .Where(e => e.AdoptedAt >= start && e.AdoptedAt < end && e.Status == AdoptionStatus.Adopted)
            .GroupBy(e => e.AdoptedAt!.Value.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var welfare = await dbContext.AnimalWelfareCases.AsNoTracking()
            .Where(e => e.CreatedAt >= start && e.CreatedAt < end
                && (string.IsNullOrWhiteSpace(canton) || e.Canton == canton))
            .GroupBy(e => e.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var points = Enumerable.Range(0, periodEnd.DayNumber - periodStart.DayNumber + 1)
            .Select(offset => periodStart.AddDays(offset))
            .Select(date => new NalaTrendPointDto(
                date,
                lost.FirstOrDefault(x => DateOnly.FromDateTime(x.Date) == date)?.Count ?? 0,
                reunited.FirstOrDefault(x => DateOnly.FromDateTime(x.Date) == date)?.Count ?? 0,
                captures.FirstOrDefault(x => DateOnly.FromDateTime(x.Date) == date)?.Count ?? 0,
                adopted.FirstOrDefault(x => DateOnly.FromDateTime(x.Date) == date)?.Count ?? 0,
                welfare.FirstOrDefault(x => DateOnly.FromDateTime(x.Date) == date)?.Count ?? 0))
            .ToList();
        return points;
    }

    public async Task<IReadOnlyList<NalaCantonSummaryDto>> GetCantonSummaryAsync(
        DateOnly periodStart,
        DateOnly periodEnd,
        int suppressionThreshold,
        CancellationToken cancellationToken = default)
    {
        var (start, end) = ToUtcRange(periodStart, periodEnd);
        var captures = await dbContext.CapturedAnimals.AsNoTracking()
            .Where(e => e.CapturedAt >= start && e.CapturedAt < end)
            .GroupBy(e => e.Canton)
            .Select(g => new { Canton = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var welfare = await dbContext.AnimalWelfareCases.AsNoTracking()
            .Where(e => e.CreatedAt >= start && e.CreatedAt < end)
            .GroupBy(e => e.Canton)
            .Select(g => new { Canton = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var reunited = await dbContext.LostPetEvents.AsNoTracking()
            .Where(e => e.Status == LostPetStatus.Reunited && e.ResolvedAt >= start && e.ResolvedAt < end)
            .GroupBy(e => e.CantonName ?? "Unknown")
            .Select(g => new { Canton = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var adopted = await dbContext.AdoptableAnimals.AsNoTracking()
            .Where(e => e.Status == AdoptionStatus.Adopted && e.AdoptedAt >= start && e.AdoptedAt < end)
            .GroupBy(e => e.RefLabel ?? "Unknown")
            .Select(g => new { Canton = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return captures.Select(x => x.Canton)
            .Concat(welfare.Select(x => x.Canton))
            .Concat(reunited.Select(x => x.Canton))
            .Concat(adopted.Select(x => x.Canton))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .Select(canton =>
            {
                var captureCount = captures.FirstOrDefault(x => x.Canton == canton)?.Count ?? 0;
                var welfareCount = welfare.FirstOrDefault(x => x.Canton == canton)?.Count ?? 0;
                var reunitedCount = reunited.FirstOrDefault(x => x.Canton == canton)?.Count ?? 0;
                var adoptedCount = adopted.FirstOrDefault(x => x.Canton == canton)?.Count ?? 0;
                var total = captureCount + welfareCount + reunitedCount + adoptedCount;
                return new NalaCantonSummaryDto(canton, captureCount, reunitedCount, adoptedCount, welfareCount, total < suppressionThreshold);
            })
            .ToList();
    }

    public async Task<NalaInstitutionPerformanceDto> GetInstitutionPerformanceAsync(
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken = default)
    {
        var (start, end) = ToUtcRange(periodStart, periodEnd);
        var verifiedClinics = await dbContext.ClinicVerifications.AsNoTracking().CountAsync(e => e.IsActive, cancellationToken);
        var activeMunicipalities = await dbContext.MunicipalityProfiles.AsNoTracking().CountAsync(e => e.IsActive, cancellationToken);
        var verifiedAllies = await dbContext.AllyProfiles.AsNoTracking().CountAsync(e => e.VerificationStatus == Domain.Allies.AllyVerificationStatus.Verified, cancellationToken);
        var openCases = await dbContext.AnimalWelfareCases.AsNoTracking().CountAsync(e => e.Status != WelfareCaseStatus.Resolved && e.Status != WelfareCaseStatus.Dismissed && e.Status != WelfareCaseStatus.ClosedNoAction, cancellationToken);
        var assignedCases = await dbContext.AnimalWelfareCases.AsNoTracking().CountAsync(e => e.AssignedOrganizationUserId != null && e.CreatedAt >= start && e.CreatedAt < end, cancellationToken);
        var closureHours = await dbContext.AnimalWelfareCases.AsNoTracking()
            .Where(e => e.ClosedAt >= start && e.ClosedAt < end)
            .Select(e => e.ClosedAt!.Value.Subtract(e.CreatedAt).TotalHours)
            .ToListAsync(cancellationToken);
        return new NalaInstitutionPerformanceDto(
            verifiedClinics,
            activeMunicipalities,
            verifiedAllies,
            openCases,
            assignedCases,
            closureHours.Count == 0 ? null : closureHours.OrderBy(x => x).ElementAt(closureHours.Count / 2));
    }

    private static (DateTimeOffset Start, DateTimeOffset End) ToUtcRange(DateOnly periodStart, DateOnly periodEnd)
    {
        var start = new DateTimeOffset(periodStart.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var end = new DateTimeOffset(periodEnd.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        return (start, end);
    }

    private static List<RegulatoryReportRow> OverviewRows(NalaOverviewDto overview) =>
    [
        new("ActiveLostPets", "Nacional", overview.ActiveLostPets, false),
        new("ReunitedPets", "Nacional", overview.ReunitedPets, false),
        new("CapturedAnimals", "Nacional", overview.CapturedAnimals, false),
        new("AvailableAdoptions", "Nacional", overview.AvailableAdoptions, false),
        new("AdoptedAnimals", "Nacional", overview.AdoptedAnimals, false),
        new("OpenWelfareCases", "Nacional", overview.OpenWelfareCases, false),
        new("VerifiedMicrochips", "Nacional", overview.VerifiedMicrochips, false),
        new("ValidCertificates", "Nacional", overview.ValidCertificates, false),
    ];

    private static List<RegulatoryReportRow> ApplyThreshold(
        IEnumerable<(string Category, string Dimension, int Count)> source,
        int threshold) => source
        .Select(item => item.Count < threshold
            ? new RegulatoryReportRow(item.Category, item.Dimension, 0, true)
            : new RegulatoryReportRow(item.Category, item.Dimension, item.Count, false))
        .ToList();
}

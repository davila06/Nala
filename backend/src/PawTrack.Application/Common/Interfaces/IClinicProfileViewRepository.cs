using PawTrack.Domain.Clinics;

namespace PawTrack.Application.Common.Interfaces;

public interface IClinicProfileViewRepository
{
    Task AddAsync(ClinicProfileView view, CancellationToken ct = default);
    Task<ClinicVisibilityStatsDto> GetStatsAsync(Guid clinicId, int days, CancellationToken ct = default);
    Task PruneOlderThanAsync(int days, CancellationToken ct = default);
}

public sealed record ClinicVisibilityStatsDto(
    int PeriodDays,
    int ProfileViews,
    int MapClicks,
    int SearchAppearances,
    int AlertImpressions,
    int ScanResultViews);

using PawTrack.Domain.Clinics;

namespace PawTrack.Application.Common.Interfaces;

public interface IClinicScanRepository
{
    Task AddAsync(ClinicScan scan, CancellationToken cancellationToken = default);
    Task<ClinicScanMonthlyStats> GetMonthlyStatsAsync(Guid clinicId, int year, int month, CancellationToken cancellationToken = default);

    /// <summary>True if this clinic scanned the given pet within the last <paramref name="withinDays"/> days (Option A gate).</summary>
    Task<bool> HasRecentScanAsync(Guid clinicId, Guid petId, int withinDays = 90, CancellationToken ct = default);

    /// <summary>Returns the most recent scan entry for this clinic+pet combination, or null.</summary>
    Task<DateTimeOffset?> GetLastScanDateAsync(Guid clinicId, Guid petId, CancellationToken ct = default);
}

public sealed record ClinicScanDayCount(DateOnly Day, int Total, int Matched, int QrCount, int RfidCount);

public sealed record ClinicScanMonthlyStats(
    int TotalScans,
    int MatchedScans,
    int QrScans,
    int RfidScans,
    IReadOnlyList<ClinicScanDayCount> ByDay);

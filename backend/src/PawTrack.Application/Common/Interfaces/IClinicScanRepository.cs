using PawTrack.Domain.Clinics;

namespace PawTrack.Application.Common.Interfaces;

public interface IClinicScanRepository
{
    Task AddAsync(ClinicScan scan, CancellationToken cancellationToken = default);
    Task<ClinicScanMonthlyStats> GetMonthlyStatsAsync(Guid clinicId, int year, int month, CancellationToken cancellationToken = default);
}

public sealed record ClinicScanDayCount(DateOnly Day, int Total, int Matched, int QrCount, int RfidCount);

public sealed record ClinicScanMonthlyStats(
    int TotalScans,
    int MatchedScans,
    int QrScans,
    int RfidScans,
    IReadOnlyList<ClinicScanDayCount> ByDay);

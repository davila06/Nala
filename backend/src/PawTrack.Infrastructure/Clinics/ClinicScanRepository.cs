using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Clinics;

public sealed class ClinicScanRepository(PawTrackDbContext dbContext) : IClinicScanRepository
{
    public async Task AddAsync(ClinicScan scan, CancellationToken cancellationToken = default) =>
        await dbContext.ClinicScans.AddAsync(scan, cancellationToken);

    public async Task<bool> HasRecentScanAsync(
        Guid clinicId, Guid petId, int withinDays = 90, CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-withinDays);
        return await dbContext.ClinicScans
            .AsNoTracking()
            .AnyAsync(s => s.ClinicId == clinicId
                        && s.MatchedPetId == petId
                        && s.ScannedAt >= cutoff, ct);
    }

    public async Task<DateTimeOffset?> GetLastScanDateAsync(
        Guid clinicId, Guid petId, CancellationToken ct = default)
    {
        return await dbContext.ClinicScans
            .AsNoTracking()
            .Where(s => s.ClinicId == clinicId && s.MatchedPetId == petId)
            .OrderByDescending(s => s.ScannedAt)
            .Select(s => (DateTimeOffset?)s.ScannedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ClinicScanMonthlyStats> GetMonthlyStatsAsync(
        Guid clinicId, int year, int month, CancellationToken cancellationToken = default)
    {
        var scans = await dbContext.ClinicScans
            .AsNoTracking()
            .Where(s => s.ClinicId == clinicId
                     && s.ScannedAt.Year == year
                     && s.ScannedAt.Month == month)
            .ToListAsync(cancellationToken);

        var byDay = scans
            .GroupBy(s => DateOnly.FromDateTime(s.ScannedAt.LocalDateTime))
            .OrderBy(g => g.Key)
            .Select(g => new ClinicScanDayCount(
                g.Key,
                g.Count(),
                g.Count(s => s.MatchedPetId.HasValue),
                g.Count(s => s.InputType == ScanInputType.Qr),
                g.Count(s => s.InputType == ScanInputType.RfidChip)))
            .ToList();

        return new ClinicScanMonthlyStats(
            scans.Count,
            scans.Count(s => s.MatchedPetId.HasValue),
            scans.Count(s => s.InputType == ScanInputType.Qr),
            scans.Count(s => s.InputType == ScanInputType.RfidChip),
            byDay);
    }
}


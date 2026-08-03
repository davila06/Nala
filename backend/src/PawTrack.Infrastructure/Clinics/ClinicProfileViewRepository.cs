using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Clinics;

public sealed class ClinicProfileViewRepository(PawTrackDbContext dbContext) : IClinicProfileViewRepository
{
    public async Task AddAsync(ClinicProfileView view, CancellationToken ct = default) =>
        await dbContext.ClinicProfileViews.AddAsync(view, ct);

    public async Task<ClinicVisibilityStatsDto> GetStatsAsync(
        Guid clinicId, int days, CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-days);
        var views = await dbContext.ClinicProfileViews
            .AsNoTracking()
            .Where(v => v.ClinicId == clinicId && v.ViewedAt >= cutoff)
            .ToListAsync(ct);

        return new ClinicVisibilityStatsDto(
            PeriodDays: days,
            ProfileViews: views.Count(v => v.Source == "directory"),
            MapClicks: views.Count(v => v.Source == "map"),
            SearchAppearances: views.Count(v => v.Source == "search"),
            AlertImpressions: views.Count(v => v.Source == "alert"),
            ScanResultViews: views.Count(v => v.Source == "scan_result"));
    }

    public async Task PruneOlderThanAsync(int days, CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-days);
        await dbContext.ClinicProfileViews
            .Where(v => v.ViewedAt < cutoff)
            .ExecuteDeleteAsync(ct);
    }
}

using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Medical;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Medical;

public sealed class HealthReportExportRepository(PawTrackDbContext db) : IHealthReportExportRepository
{
    public async Task AddAsync(HealthReportExport export, CancellationToken ct = default) =>
        await db.HealthReportExports.AddAsync(export, ct);

    public Task<HealthReportExport?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.HealthReportExports.FirstOrDefaultAsync(export => export.Id == id, ct);

    public Task<HealthReportExport?> GetReusableAsync(Guid requestedByUserId, Guid petId, DateTimeOffset now, CancellationToken ct = default) =>
        db.HealthReportExports.AsNoTracking()
            .Where(export => export.RequestedByUserId == requestedByUserId && export.PetId == petId &&
                (export.Status == HealthReportExportStatus.Queued || export.Status == HealthReportExportStatus.Processing ||
                 export.Status == HealthReportExportStatus.Completed && export.ExpiresAt > now))
            .OrderByDescending(export => export.RequestedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<HealthReportExport>> GetQueuedAsync(int take, CancellationToken ct = default) =>
        await db.HealthReportExports.Where(export => export.Status == HealthReportExportStatus.Queued)
            .OrderBy(export => export.RequestedAt).Take(Math.Clamp(take, 1, 100)).ToListAsync(ct);

    public async Task<IReadOnlyList<HealthReportExport>> GetStaleProcessingAsync(DateTimeOffset staleBefore, int take, CancellationToken ct = default) =>
        await db.HealthReportExports.Where(export => export.Status == HealthReportExportStatus.Processing &&
                export.StartedAt != null && export.StartedAt < staleBefore)
            .OrderBy(export => export.StartedAt).Take(Math.Clamp(take, 1, 100)).ToListAsync(ct);

    public async Task<IReadOnlyList<HealthReportExport>> GetExpiredAsync(DateTimeOffset now, int take, CancellationToken ct = default) =>
        await db.HealthReportExports.Where(export => export.Status == HealthReportExportStatus.Completed && export.ExpiresAt <= now)
            .OrderBy(export => export.ExpiresAt).Take(Math.Clamp(take, 1, 100)).ToListAsync(ct);

    public void Update(HealthReportExport export) => db.HealthReportExports.Update(export);
}

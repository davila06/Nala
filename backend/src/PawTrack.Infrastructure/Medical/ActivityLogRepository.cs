using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Medical;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Medical;

public sealed class ActivityLogRepository(PawTrackDbContext db) : IActivityLogRepository
{
    public async Task<IReadOnlyList<ActivityLog>> GetByPetAndDateRangeAsync(
        Guid petId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        await db.ActivityLogs.AsNoTracking()
            .Where(a => a.PetId == petId && a.Date >= from && a.Date <= to)
            .OrderByDescending(a => a.Date)
            .ToListAsync(ct);

    public Task<ActivityLog?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.ActivityLogs.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task AddAsync(ActivityLog log, CancellationToken ct = default) =>
        await db.ActivityLogs.AddAsync(log, ct);

    public void Delete(ActivityLog log) => db.ActivityLogs.Remove(log);

    public Task<bool> ExistsTractiveForDateAsync(Guid petId, DateOnly date, CancellationToken ct = default) =>
        db.ActivityLogs.AnyAsync(
            a => a.PetId == petId && a.Date == date && a.Source == ActivitySource.Tractive, ct);
}

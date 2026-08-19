using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Advertising;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Advertising;

public sealed class BillboardRepository(PawTrackDbContext db) : IBillboardRepository
{
    public Task<Billboard?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Billboards.FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<IReadOnlyList<Billboard>> GetActiveByPlacementAsync(
        BillboardPlacement placement, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        return await db.Billboards.AsNoTracking()
            .Where(b => b.Placement == placement &&
                        b.Status == BillboardStatus.Active &&
                        b.StartsAt <= now && b.EndsAt > now)
            .OrderByDescending(b => b.Priority)
            .Take(5) // cap to avoid flooding the UI
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Billboard>> GetAllAsync(int skip, int take, CancellationToken ct = default) =>
        await db.Billboards.AsNoTracking()
            .OrderByDescending(b => b.CreatedAt)
            .Skip(skip).Take(take)
            .ToListAsync(ct);

    public Task<int> CountAllAsync(CancellationToken ct = default) =>
        db.Billboards.CountAsync(ct);

    public async Task AddAsync(Billboard billboard, CancellationToken ct = default) =>
        await db.Billboards.AddAsync(billboard, ct);

    public void Update(Billboard billboard) => db.Billboards.Update(billboard);
}

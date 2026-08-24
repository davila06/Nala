using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Pets;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Pets;

public sealed class QrScanEventRepository(PawTrackDbContext dbContext) : IQrScanEventRepository
{
    public async Task AddAsync(QrScanEvent scanEvent, CancellationToken cancellationToken = default) =>
        await dbContext.Set<QrScanEvent>().AddAsync(scanEvent, cancellationToken);

    public async Task<IReadOnlyList<QrScanEvent>> GetByPetIdAsync(
        Guid petId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Set<QrScanEvent>()
            .AsNoTracking()
            .Where(e => e.PetId == petId)
            .OrderByDescending(e => e.ScannedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return rows.AsReadOnly();
    }

    public async Task<bool> HasScanForPetOnDateAsync(
        Guid petId,
        DateOnly utcDate,
        CancellationToken cancellationToken = default)
    {
        var start = new DateTimeOffset(
            utcDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            TimeSpan.Zero);
        var end = start.AddDays(1);

        return await dbContext.Set<QrScanEvent>()
            .AnyAsync(e =>
                    e.PetId == petId
                    && e.ScannedAt >= start
                    && e.ScannedAt < end,
                cancellationToken);
    }

    public Task<bool> HasScanForPetSinceAsync(
        Guid petId,
        DateTimeOffset fromUtc,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Set<QrScanEvent>()
            .AnyAsync(e => e.PetId == petId && e.ScannedAt >= fromUtc, cancellationToken);

    }

    public async Task<int> DeleteBeforeAsync(
        DateTimeOffset cutoff,
        CancellationToken cancellationToken = default)
    {
        // ExecuteDeleteAsync issues a single DELETE ... WHERE statement — no change tracking overhead.
        return await dbContext.Set<QrScanEvent>()
            .Where(e => e.ScannedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<QrScanEvent>> GetByPetIdAfterCursorAsync(
        Guid petId, Guid? afterId, int take, CancellationToken cancellationToken = default)
    {
        // Guid v7 IDs embed a timestamp — descending Id order matches descending ScannedAt.
        var q = dbContext.Set<QrScanEvent>().AsNoTracking().Where(e => e.PetId == petId);
        if (afterId.HasValue)
            q = q.Where(e => e.Id.CompareTo(afterId.Value) < 0);
        return await q.OrderByDescending(e => e.Id).Take(take).ToListAsync(cancellationToken);
    }
}

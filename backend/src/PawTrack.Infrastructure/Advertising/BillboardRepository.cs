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

    public Task<bool> HasDeliveryEventAsync(Guid billboardId, BillboardDeliveryEventType eventType, string eventKeyHash, CancellationToken ct = default) =>
        db.BillboardDeliveryEvents.AnyAsync(e => e.BillboardId == billboardId && e.EventType == eventType && e.EventKeyHash == eventKeyHash, ct);

    public Task<int> CountImpressionsByVisitorTodayAsync(Guid billboardId, string visitorHash, CancellationToken ct = default) =>
        db.BillboardDeliveryEvents.AsNoTracking().CountAsync(e => e.BillboardId == billboardId &&
            e.VisitorHash == visitorHash && e.OccurredOn == DateOnly.FromDateTime(DateTime.UtcNow) &&
            e.EventType == BillboardDeliveryEventType.Impression, ct);

    public async Task AddDeliveryEventAsync(BillboardDeliveryEvent deliveryEvent, CancellationToken ct = default) =>
        await db.BillboardDeliveryEvents.AddAsync(deliveryEvent, ct);

    public async Task<BillboardCampaignMetrics> GetMetricsAsync(Guid billboardId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var events = await db.BillboardDeliveryEvents.AsNoTracking()
            .Where(e => e.BillboardId == billboardId && e.OccurredOn >= from && e.OccurredOn <= to)
            .ToListAsync(ct);
        var impressions = events.Count(e => e.EventType == BillboardDeliveryEventType.Impression);
        var clicks = events.Count(e => e.EventType == BillboardDeliveryEventType.Click);
        var cantons = events.Where(e => e.EventType == BillboardDeliveryEventType.Impression)
            .GroupBy(e => e.Canton ?? "Sin zona").ToDictionary(g => g.Key, g => g.Count());
        var billboard = await db.Billboards.AsNoTracking().FirstOrDefaultAsync(b => b.Id == billboardId, ct);
        var placements = billboard is null ? new Dictionary<string, int>() : new Dictionary<string, int> { [billboard.Placement.ToString()] = impressions };
        return new BillboardCampaignMetrics(billboardId, impressions, clicks,
            events.Count(e => e.EventType == BillboardDeliveryEventType.Conversion),
            impressions == 0 ? 0 : Math.Round((decimal)clicks / impressions * 100, 2), cantons, placements);
    }

    public async Task AddAsync(Billboard billboard, CancellationToken ct = default) =>
        await db.Billboards.AddAsync(billboard, ct);

    public async Task DeleteAsync(Billboard billboard, CancellationToken ct = default)
    {
        await db.BillboardDeliveryEvents.Where(e => e.BillboardId == billboard.Id).ExecuteDeleteAsync(ct);
        db.Billboards.Remove(billboard);
    }

    public void Update(Billboard billboard) => db.Billboards.Update(billboard);
}

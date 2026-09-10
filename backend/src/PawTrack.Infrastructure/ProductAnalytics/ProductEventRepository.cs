using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.ProductAnalytics;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.ProductAnalytics;

public sealed class ProductEventRepository(PawTrackDbContext db) : IProductEventRepository
{
    public Task<bool> ExistsByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        db.ProductEvents.AsNoTracking().AnyAsync(x => x.EventId == eventId, cancellationToken);

    public async Task AddAsync(ProductEvent productEvent, CancellationToken cancellationToken = default) =>
        await db.ProductEvents.AddAsync(productEvent, cancellationToken);

    public Task<int> DeleteOccurredBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default) =>
        db.ProductEvents.Where(x => x.OccurredAt < cutoff).ExecuteDeleteAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductEventCount>> CountByEventNameAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string? canton,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.ProductEvents.AsNoTracking()
            .Where(x => x.OccurredAt >= from && x.OccurredAt < to);

        if (!string.IsNullOrWhiteSpace(canton))
            query = query.Where(x => x.Canton == canton);

        if (!string.IsNullOrWhiteSpace(correlationId))
            query = query.Where(x => x.CorrelationId == correlationId);

        var rows = await query
            .GroupBy(x => x.EventName)
            .Select(group => new
            {
                EventName = group.Key,
                Count = group.Count(),
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new ProductEventCount(row.EventName, row.Count))
            .ToList();
    }
}
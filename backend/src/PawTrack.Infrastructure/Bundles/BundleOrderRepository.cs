using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Bundles.Interfaces;
using PawTrack.Domain.Bundles;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Bundles;

public sealed class BundleOrderRepository(PawTrackDbContext dbContext) : IBundleOrderRepository
{
    public Task<BundleOrder?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        dbContext.BundleOrders.FindAsync([id], ct).AsTask();

    public async Task<IReadOnlyList<BundleOrder>> GetByUserIdAsync(
        Guid userId, CancellationToken ct = default) =>
        await dbContext.BundleOrders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<BundleOrder>> GetAllPagedAsync(
        BundleOrderStatus? statusFilter, int skip, int take, CancellationToken ct = default)
    {
        var q = dbContext.BundleOrders.AsNoTracking();
        if (statusFilter.HasValue)
            q = q.Where(o => o.Status == statusFilter.Value);
        return await q.OrderByDescending(o => o.CreatedAt).Skip(skip).Take(take).ToListAsync(ct);
    }

    public async Task<int> CountAllAsync(BundleOrderStatus? statusFilter, CancellationToken ct = default)
    {
        var q = dbContext.BundleOrders.AsNoTracking();
        if (statusFilter.HasValue)
            q = q.Where(o => o.Status == statusFilter.Value);
        return await q.CountAsync(ct);
    }

    public async Task AddAsync(BundleOrder order, CancellationToken ct = default) =>
        await dbContext.BundleOrders.AddAsync(order, ct);

    public void Update(BundleOrder order) =>
        dbContext.BundleOrders.Update(order);
}

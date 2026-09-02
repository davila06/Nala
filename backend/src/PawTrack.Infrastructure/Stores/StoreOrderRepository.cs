using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Stores;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Stores;

public sealed class StoreOrderRepository(PawTrackDbContext db) : IStoreOrderRepository
{
    public Task<StoreOrder?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.StoreOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<StoreOrder?> GetByPaymentReferenceAsync(string reference, CancellationToken ct = default) =>
        db.StoreOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.PaymentReference == reference, ct);

    public async Task<IReadOnlyList<StoreOrder>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default) =>
        await db.StoreOrders.AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.PlacedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StoreOrder>> GetByCustomerPagedAsync(
        Guid customerId, int skip, int take, CancellationToken ct = default) =>
        await db.StoreOrders.AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.PlacedAt)
            .Skip(skip).Take(take)
            .ToListAsync(ct);

    public Task<int> CountByCustomerAsync(Guid customerId, CancellationToken ct = default) =>
        db.StoreOrders.CountAsync(o => o.CustomerId == customerId, ct);

    public async Task<IReadOnlyList<StoreOrder>> GetByStoreAsync(Guid storeId, int page, int pageSize, CancellationToken ct = default) =>
        await db.StoreOrders.AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.StoreId == storeId)
            .OrderByDescending(o => o.PlacedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<StoreOrderMonthlyStats> GetMonthlyStatsAsync(
        Guid storeId, int year, int month, Guid? locationId = null, CancellationToken ct = default)
    {
        var orders = await db.StoreOrders.AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.StoreId == storeId
                     && o.PlacedAt.Year == year
                     && o.PlacedAt.Month == month
                     && (locationId == null || o.LocationId == locationId))
            .ToListAsync(ct);

        var delivered = orders.Where(o => o.Status == StoreOrderStatus.Delivered).ToList();
        var cancelled = orders.Count(o => o.Status == StoreOrderStatus.Cancelled);
        var totalRevenue = delivered.Sum(o => o.TotalCrc);

        var byDay = orders
            .GroupBy(o => DateOnly.FromDateTime(o.PlacedAt.LocalDateTime))
            .OrderBy(g => g.Key)
            .Select(g => new StoreOrderDayStat(
                g.Key,
                g.Count(),
                g.Where(o => o.Status == StoreOrderStatus.Delivered).Sum(o => o.TotalCrc)))
            .ToList();

        var topProducts = delivered
            .SelectMany(o => o.Items)
            .GroupBy(i => new { i.ProductId, i.ProductName })
            .Select(g => new StoreTopProductStat(
                g.Key.ProductId,
                g.Key.ProductName,
                g.Sum(i => i.Quantity),
                g.Sum(i => i.SubtotalCrc)))
            .OrderByDescending(p => p.RevenueCrc)
            .Take(5)
            .ToList();

        return new StoreOrderMonthlyStats(
            orders.Count,
            delivered.Count,
            cancelled,
            totalRevenue,
            delivered.Count > 0 ? totalRevenue / delivered.Count : 0m,
            byDay,
            topProducts);
    }

    public async Task AddAsync(StoreOrder order, CancellationToken ct = default) =>
        await db.StoreOrders.AddAsync(order, ct);

    public void Update(StoreOrder order) => db.StoreOrders.Update(order);
}

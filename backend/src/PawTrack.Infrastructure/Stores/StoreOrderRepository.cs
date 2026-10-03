using Microsoft.EntityFrameworkCore;
using System.Data;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Stores;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Stores;

public sealed class StoreOrderRepository(PawTrackDbContext db) : IStoreOrderRepository
{
    public Task<int> CountByStoreSinceAsync(Guid storeId, DateTimeOffset since, CancellationToken ct = default) =>
        db.StoreOrders.CountAsync(order => order.StoreId == storeId && order.PlacedAt >= since, ct);

    public Task<StoreOrder?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.StoreOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<StoreOrder?> GetByPaymentReferenceAsync(string reference, CancellationToken ct = default) =>
        db.StoreOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.PaymentReference == reference, ct);

    public Task<StoreOrder?> GetByCustomerAndIdempotencyKeyAsync(
        Guid customerId, string idempotencyKey, CancellationToken ct = default) =>
        db.StoreOrders.AsNoTracking()
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.CustomerId == customerId && order.IdempotencyKey == idempotencyKey, ct);

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

    public async Task<bool> TryUpdateStatusAndReleaseStockAsync(
        Guid orderId, Guid storeId, StoreOrderStatus newStatus, string? storeNote, CancellationToken ct = default)
    {
        if (newStatus is not (StoreOrderStatus.Cancelled or StoreOrderStatus.Rejected))
            throw new ArgumentOutOfRangeException(nameof(newStatus), "This operation only supports cancellation or rejection.");

        if (!db.Database.IsRelational())
            return await UpdateStatusAndReleaseStockCoreAsync(orderId, storeId, newStatus, storeNote, ct);

        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var updated = await UpdateStatusAndReleaseStockCoreAsync(orderId, storeId, newStatus, storeNote, ct);
            if (!updated)
            {
                await transaction.RollbackAsync(ct);
                return false;
            }

            await transaction.CommitAsync(ct);
            return true;
        });
    }

    private async Task<bool> UpdateStatusAndReleaseStockCoreAsync(
        Guid orderId, Guid storeId, StoreOrderStatus newStatus, string? storeNote, CancellationToken ct)
    {
        var order = await db.StoreOrders.Include(candidate => candidate.Items)
            .FirstOrDefaultAsync(candidate => candidate.Id == orderId && candidate.StoreId == storeId, ct);
        if (order is null) return false;

        Dictionary<Guid, StoreProduct>? products = null;
        if (order.StockReserved)
        {
            var productIds = order.Items.Select(item => item.ProductId).Distinct().ToArray();
            products = await db.StoreProducts.AsTracking()
                .Where(product => product.StoreId == storeId && productIds.Contains(product.Id))
                .ToDictionaryAsync(product => product.Id, ct);
            if (products.Count != productIds.Length) return false;
        }

        try { order.UpdateStatus(newStatus, storeNote); }
        catch (InvalidOperationException) { return false; }

        if (products is not null && order.ReleaseStockReservation())
            foreach (var item in order.Items)
                products[item.ProductId].ReleaseStock(item.Quantity);

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> TryAcceptAndReserveStockAsync(
        Guid orderId, Guid storeId, string? storeNote, DateTimeOffset reservationExpiresAt, CancellationToken ct = default)
    {
        if (!db.Database.IsRelational())
            return await TryAcceptAndReserveStockCoreAsync(orderId, storeId, storeNote, reservationExpiresAt, ct);

        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var accepted = await TryAcceptAndReserveStockCoreAsync(orderId, storeId, storeNote, reservationExpiresAt, ct);
            if (!accepted)
            {
                await transaction.RollbackAsync(ct);
                return false;
            }

            await transaction.CommitAsync(ct);
            return true;
        });
    }

    private async Task<bool> TryAcceptAndReserveStockCoreAsync(
        Guid orderId, Guid storeId, string? storeNote, DateTimeOffset reservationExpiresAt, CancellationToken ct)
    {
        var order = await db.StoreOrders
            .Include(candidate => candidate.Items)
            .FirstOrDefaultAsync(candidate => candidate.Id == orderId && candidate.StoreId == storeId, ct);
        if (order is null) return false;
        if (order.StockReserved &&
            order.Status is StoreOrderStatus.AwaitingPayment or StoreOrderStatus.PaymentReported)
            return true;
        if (order.Status is not (StoreOrderStatus.AwaitingStoreAcceptance or StoreOrderStatus.PendingPayment or StoreOrderStatus.PaymentReported) ||
            order.Items.Count == 0)
            return false;

        var productIds = order.Items.Select(item => item.ProductId).Distinct().ToArray();
        var products = await db.StoreProducts.AsTracking()
            .Where(product => product.StoreId == storeId && productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, ct);
        if (products.Count != productIds.Length ||
            order.Items.Any(item => !products.TryGetValue(item.ProductId, out var product) ||
                                    !product.IsAvailable || product.StockOnHand is null ||
                                    product.StockOnHand.Value < item.Quantity))
            return false;

        foreach (var item in order.Items)
            products[item.ProductId].ReserveStock(item.Quantity);

        order.MarkStockReserved(reservationExpiresAt);
        order.Accept(storeNote);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<Guid>> GetExpiredStockReservationOrderIdsAsync(
        DateTimeOffset now, int take, CancellationToken ct = default) =>
        await db.StoreOrders.AsNoTracking()
            .Where(order => order.StockReserved && order.StockReservationExpiresAt <= now &&
                            (order.Status == StoreOrderStatus.AwaitingPayment || order.Status == StoreOrderStatus.PaymentReported))
            .OrderBy(order => order.StockReservationExpiresAt)
            .Select(order => order.Id)
            .Take(Math.Clamp(take, 1, 500))
            .ToListAsync(ct);

    public async Task<bool> ExpireStockReservationAsync(Guid orderId, DateTimeOffset now, CancellationToken ct = default)
    {
        if (!db.Database.IsRelational())
            return await ExpireStockReservationCoreAsync(orderId, now, ct);

        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var expired = await ExpireStockReservationCoreAsync(orderId, now, ct);
            if (!expired)
            {
                await transaction.RollbackAsync(ct);
                return false;
            }

            await transaction.CommitAsync(ct);
            return true;
        });
    }

    private async Task<bool> ExpireStockReservationCoreAsync(Guid orderId, DateTimeOffset now, CancellationToken ct)
    {
        var order = await db.StoreOrders.Include(candidate => candidate.Items)
            .FirstOrDefaultAsync(candidate => candidate.Id == orderId, ct);
        if (order is null || !order.StockReserved || order.StockReservationExpiresAt > now)
            return false;

        var productIds = order.Items.Select(item => item.ProductId).Distinct().ToArray();
        var products = await db.StoreProducts.AsTracking()
            .Where(product => product.StoreId == order.StoreId && productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, ct);
        if (products.Count != productIds.Length) return false;

        order.ExpireStockReservation(now);
        foreach (var item in order.Items)
            products[item.ProductId].ReleaseStock(item.Quantity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

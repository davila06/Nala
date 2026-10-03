using PawTrack.Domain.Stores;

namespace PawTrack.Application.Common.Interfaces;

public sealed record StoreOrderDayStat(DateOnly Day, int OrderCount, decimal RevenueCrc);

public sealed record StoreTopProductStat(Guid ProductId, string ProductName, int QuantitySold, decimal RevenueCrc);

public sealed record StoreOrderMonthlyStats(
    int TotalOrders,
    int DeliveredOrders,
    int CancelledOrders,
    decimal TotalRevenueCrc,
    decimal AverageOrderValueCrc,
    IReadOnlyList<StoreOrderDayStat> ByDay,
    IReadOnlyList<StoreTopProductStat> TopProducts);

public interface IStoreOrderRepository
{
    Task<StoreOrder?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<StoreOrder?> GetByPaymentReferenceAsync(string reference, CancellationToken ct = default);
    Task<StoreOrder?> GetByCustomerAndIdempotencyKeyAsync(Guid customerId, string idempotencyKey, CancellationToken ct = default);
    Task<IReadOnlyList<StoreOrder>> GetByCustomerPagedAsync(Guid customerId, int skip, int take, CancellationToken ct = default);
    Task<int> CountByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<StoreOrder>> GetByStoreAsync(Guid storeId, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountByStoreSinceAsync(Guid storeId, DateTimeOffset since, CancellationToken ct = default);
    Task<StoreOrderMonthlyStats> GetMonthlyStatsAsync(
        Guid storeId, int year, int month, Guid? locationId = null, CancellationToken ct = default);
    Task AddAsync(StoreOrder order, CancellationToken ct = default);
    void Update(StoreOrder order);
    Task<bool> TryAcceptAndReserveStockAsync(
        Guid orderId, Guid storeId, string? storeNote, DateTimeOffset reservationExpiresAt, CancellationToken ct = default);
    Task<bool> TryUpdateStatusAndReleaseStockAsync(
        Guid orderId, Guid storeId, StoreOrderStatus newStatus, string? storeNote, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetExpiredStockReservationOrderIdsAsync(DateTimeOffset now, int take, CancellationToken ct = default);
    Task<bool> ExpireStockReservationAsync(Guid orderId, DateTimeOffset now, CancellationToken ct = default);
}

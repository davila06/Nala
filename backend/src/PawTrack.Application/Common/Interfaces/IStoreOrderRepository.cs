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
    Task<IReadOnlyList<StoreOrder>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<StoreOrder>> GetByCustomerPagedAsync(Guid customerId, int skip, int take, CancellationToken ct = default);
    Task<int> CountByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<StoreOrder>> GetByStoreAsync(Guid storeId, int page, int pageSize, CancellationToken ct = default);
    Task<StoreOrderMonthlyStats> GetMonthlyStatsAsync(Guid storeId, int year, int month, CancellationToken ct = default);
    Task AddAsync(StoreOrder order, CancellationToken ct = default);
    void Update(StoreOrder order);
}

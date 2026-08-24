using PawTrack.Domain.Stores;

namespace PawTrack.Application.Common.Interfaces;

public interface IStoreOrderRepository
{
    Task<StoreOrder?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<StoreOrder?> GetByPaymentReferenceAsync(string reference, CancellationToken ct = default);
    Task<IReadOnlyList<StoreOrder>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<StoreOrder>> GetByCustomerPagedAsync(Guid customerId, int skip, int take, CancellationToken ct = default);
    Task<int> CountByCustomerAsync(Guid customerId, CancellationToken ct = default);
    Task<IReadOnlyList<StoreOrder>> GetByStoreAsync(Guid storeId, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(StoreOrder order, CancellationToken ct = default);
    void Update(StoreOrder order);
}

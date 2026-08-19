using PawTrack.Domain.Stores;

namespace PawTrack.Application.Common.Interfaces;

public interface IStoreRepository
{
    Task<Store?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Store?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Store>> GetAllActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Store>> GetActivePagedAsync(int skip, int take, CancellationToken ct = default);
    Task<int> CountActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Store>> GetPendingAsync(CancellationToken ct = default);
    Task AddAsync(Store store, CancellationToken ct = default);
    void Update(Store store);

    // Products
    Task<StoreProduct?> GetProductByIdAsync(Guid productId, CancellationToken ct = default);
    Task<IReadOnlyList<StoreProduct>> GetProductsByStoreAsync(Guid storeId, CancellationToken ct = default);
    Task<IReadOnlyList<StoreProduct>> GetAvailableProductsByStoreAsync(Guid storeId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, StoreProduct>> GetProductsByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, string>> GetStoreNamesByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task AddProductAsync(StoreProduct product, CancellationToken ct = default);
    void UpdateProduct(StoreProduct product);
    void DeleteProduct(StoreProduct product);
}

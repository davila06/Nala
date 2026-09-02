using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Stores;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Stores;

public sealed class StoreRepository(PawTrackDbContext db) : IStoreRepository
{
    public Task<Store?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Stores.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<Store?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        db.Stores.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId, ct);

    public async Task<IReadOnlyList<Store>> GetAllActiveAsync(CancellationToken ct = default) =>
        await db.Stores.AsNoTracking()
            .Where(s => s.Status == StoreStatus.Active)
            .OrderByDescending(s => s.IsFeatured).ThenBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Store>> GetActivePagedAsync(int skip, int take, CancellationToken ct = default) =>
        await db.Stores.AsNoTracking()
            .Where(s => s.Status == StoreStatus.Active)
            .OrderByDescending(s => s.IsFeatured).ThenBy(s => s.Name)
            .Skip(skip).Take(take)
            .ToListAsync(ct);

    public Task<int> CountActiveAsync(CancellationToken ct = default) =>
        db.Stores.CountAsync(s => s.Status == StoreStatus.Active, ct);

    public async Task<IReadOnlyList<Store>> GetPendingAsync(CancellationToken ct = default) =>
        await db.Stores.AsNoTracking()
            .Where(s => s.Status == StoreStatus.Pending)
            .OrderBy(s => s.RegisteredAt)
            .ToListAsync(ct);

    public async Task AddAsync(Store store, CancellationToken ct = default) =>
        await db.Stores.AddAsync(store, ct);

    public void Update(Store store) => db.Stores.Update(store);

    // ── Products ──────────────────────────────────────────────────────────────

    public Task<StoreProduct?> GetProductByIdAsync(Guid productId, CancellationToken ct = default) =>
        db.StoreProducts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId, ct);

    public async Task<IReadOnlyList<StoreProduct>> GetProductsByStoreAsync(Guid storeId, CancellationToken ct = default) =>
        await db.StoreProducts.AsNoTracking()
            .Where(p => p.StoreId == storeId)
            .OrderBy(p => p.Category).ThenBy(p => p.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StoreProduct>> GetAvailableProductsByStoreAsync(Guid storeId, CancellationToken ct = default) =>
        await db.StoreProducts.AsNoTracking()
            .Where(p => p.StoreId == storeId && p.IsAvailable)
            .OrderBy(p => p.Category).ThenBy(p => p.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<Guid, StoreProduct>> GetProductsByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var list = await db.StoreProducts.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(ct);
        return list.ToDictionary(p => p.Id);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetStoreNamesByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.Distinct().ToList();
        return await db.Stores.AsNoTracking()
            .Where(s => idList.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);
    }

    public async Task AddProductAsync(StoreProduct product, CancellationToken ct = default) =>
        await db.StoreProducts.AddAsync(product, ct);

    public void UpdateProduct(StoreProduct product) => db.StoreProducts.Update(product);
    public void DeleteProduct(StoreProduct product) => db.StoreProducts.Remove(product);

    // ── Locations ─────────────────────────────────────────────────────────────

    public Task<StoreLocation?> GetLocationByIdAsync(Guid locationId, CancellationToken ct = default) =>
        db.StoreLocations.AsNoTracking().FirstOrDefaultAsync(l => l.Id == locationId, ct);

    public async Task<IReadOnlyList<StoreLocation>> GetLocationsByStoreAsync(Guid storeId, CancellationToken ct = default) =>
        await db.StoreLocations.AsNoTracking()
            .Where(l => l.StoreId == storeId)
            .OrderByDescending(l => l.IsPrimary).ThenBy(l => l.Name)
            .ToListAsync(ct);

    public async Task AddLocationAsync(StoreLocation location, CancellationToken ct = default) =>
        await db.StoreLocations.AddAsync(location, ct);

    public void UpdateLocation(StoreLocation location) => db.StoreLocations.Update(location);
}

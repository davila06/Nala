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

    public async Task<IReadOnlyList<StoreOrder>> GetByStoreAsync(Guid storeId, int page, int pageSize, CancellationToken ct = default) =>
        await db.StoreOrders.AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.StoreId == storeId)
            .OrderByDescending(o => o.PlacedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task AddAsync(StoreOrder order, CancellationToken ct = default) =>
        await db.StoreOrders.AddAsync(order, ct);

    public void Update(StoreOrder order) => db.StoreOrders.Update(order);
}

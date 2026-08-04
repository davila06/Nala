using PawTrack.Domain.Bundles;

namespace PawTrack.Application.Bundles.Interfaces;

public interface IBundleOrderRepository
{
    Task<BundleOrder?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<BundleOrder>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<BundleOrder>> GetAllPagedAsync(
        BundleOrderStatus? statusFilter, int skip, int take, CancellationToken ct = default);
    Task<int> CountAllAsync(BundleOrderStatus? statusFilter, CancellationToken ct = default);
    Task AddAsync(BundleOrder order, CancellationToken ct = default);
    void Update(BundleOrder order);
}

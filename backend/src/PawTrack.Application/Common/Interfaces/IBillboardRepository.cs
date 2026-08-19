using PawTrack.Domain.Advertising;

namespace PawTrack.Application.Common.Interfaces;

public interface IBillboardRepository
{
    Task<Billboard?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Billboard>> GetActiveByPlacementAsync(BillboardPlacement placement, CancellationToken ct = default);
    Task<IReadOnlyList<Billboard>> GetAllAsync(int skip, int take, CancellationToken ct = default);
    Task<int> CountAllAsync(CancellationToken ct = default);
    Task AddAsync(Billboard billboard, CancellationToken ct = default);
    void Update(Billboard billboard);
}

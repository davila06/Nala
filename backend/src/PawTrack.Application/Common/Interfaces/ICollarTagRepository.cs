using PawTrack.Domain.Collars;

namespace PawTrack.Application.Common.Interfaces;

public sealed record CollarTagMetricsDto(
    int TotalSerials,
    int UnactivatedCount,
    int ActivatedCount,
    int DeactivatedCount,
    int SoldLast30Days,
    int DeadInventoryCount); // sold > 90 days ago, still never activated

public interface ICollarTagRepository
{
    Task<CollarTag?> GetBySerialAsync(string serial, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CollarTag>> GetAllAsync(int skip, int take, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task AddAsync(CollarTag tag, CancellationToken cancellationToken = default);
    void Update(CollarTag tag);

    /// <summary>Filtered/searched inventory listing for the admin dashboard.</summary>
    Task<(IReadOnlyList<CollarTag> Items, int Total)> SearchAsync(
        string? serialContains,
        CollarTagStatus? status,
        DateTimeOffset? soldAfter,
        DateTimeOffset? soldBefore,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<CollarTagMetricsDto> GetMetricsAsync(CancellationToken cancellationToken = default);
}

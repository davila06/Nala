using PawTrack.Domain.Collars;

namespace PawTrack.Application.Collars.Interfaces;

public interface ICollarRepository
{
    Task<Collar?> GetActiveForPetAsync(Guid petId, CancellationToken cancellationToken = default);
    Task<Collar?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CollarLocation>> GetLocationHistoryAsync(
        Guid collarId,
        DateTimeOffset since,
        int maxPoints,
        CancellationToken cancellationToken = default);

    /// <summary>Explicit date-range variant used by the owner-facing history/export/heatmap endpoints.</summary>
    Task<IReadOnlyList<CollarLocation>> GetLocationHistoryRangeAsync(
        Guid collarId,
        DateTimeOffset from,
        DateTimeOffset to,
        int maxPoints,
        CancellationToken cancellationToken = default);
    Task AddAsync(Collar collar, CancellationToken cancellationToken = default);
    Task AddLocationAsync(CollarLocation location, CancellationToken cancellationToken = default);
    void Update(Collar collar);

    /// <summary>Active collars with offline and/or battery alerts enabled — used by connectivity detection jobs.</summary>
    Task<IReadOnlyList<Collar>> GetActiveCollarsWithAlertsEnabledAsync(CancellationToken cancellationToken = default);
}

using PawTrack.Domain.Collars;

namespace PawTrack.Application.Collars.Interfaces;

public interface ICollarSafeZoneRepository
{
    Task AddAsync(CollarSafeZone zone, CancellationToken cancellationToken = default);
    Task<CollarSafeZone?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CollarSafeZone>> GetByCollarIdAsync(Guid collarId, CancellationToken cancellationToken = default);

    /// <summary>Only zones with <see cref="CollarSafeZone.Enabled"/> — used by the breach-evaluation path.</summary>
    Task<IReadOnlyList<CollarSafeZone>> GetEnabledByCollarIdAsync(Guid collarId, CancellationToken cancellationToken = default);

    void Update(CollarSafeZone zone);
    void Remove(CollarSafeZone zone);
}

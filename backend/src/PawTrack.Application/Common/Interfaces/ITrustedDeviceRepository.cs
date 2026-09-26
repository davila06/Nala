using PawTrack.Domain.Auth;

namespace PawTrack.Application.Common.Interfaces;

public interface ITrustedDeviceRepository
{
    Task<TrustedDevice?> GetByTokenHashAsync(Guid userId, string tokenHash, CancellationToken cancellationToken = default);
    Task<TrustedDevice?> GetByIdAsync(Guid userId, Guid deviceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TrustedDevice>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task RevokeForSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(TrustedDevice device, CancellationToken cancellationToken = default);
    void Update(TrustedDevice device);
}

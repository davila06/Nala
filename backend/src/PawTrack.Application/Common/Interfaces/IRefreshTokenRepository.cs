using PawTrack.Domain.Auth;

namespace PawTrack.Application.Common.Interfaces;

public interface IRefreshTokenRepository
{
    /// <summary>Returns an active (non-revoked, non-expired) refresh token by its SHA-256 hash.</summary>
    Task<RefreshToken?> GetActiveByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns any refresh token matching the hash, including revoked or expired ones.
    /// Used for token-theft detection: a previously rotated (revoked) token being
    /// replayed is a signal that the session was compromised.
    /// </summary>
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default);
    void Update(RefreshToken token);
}

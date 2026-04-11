namespace PawTrack.Application.Common.Interfaces;

/// <summary>
/// Stores revoked JWT access-token identifiers (jti claims) so that logged-out
/// tokens cannot be reused within their remaining lifetime.
/// </summary>
public interface IJtiBlocklist
{
    /// <summary>Adds a jti to the blocklist, expiring it at <paramref name="expiresAt"/>.</summary>
    Task AddAsync(string jti, DateTimeOffset expiresAt, CancellationToken cancellationToken);

    /// <summary>Returns <c>true</c> when the jti has been revoked and not yet expired.</summary>
    Task<bool> IsBlockedAsync(string jti, CancellationToken cancellationToken);
}

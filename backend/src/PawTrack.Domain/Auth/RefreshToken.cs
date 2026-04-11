namespace PawTrack.Domain.Auth;

public sealed class RefreshToken
{
    private RefreshToken() { } // EF Core

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty; // SHA-256 hash — nunca el token plano
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// The UTC timestamp when the <em>original</em> session was first established (first login).
    /// Preserved unchanged across all token rotations in the same session chain.
    /// The handler uses this to enforce an absolute session ceiling (e.g. 90 days).
    /// </summary>
    public DateTimeOffset SessionIssuedAt { get; private set; }

    internal static RefreshToken Create(
        Guid userId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset? sessionIssuedAt = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new()
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            IsRevoked = false,
            CreatedAt = now,
            SessionIssuedAt = sessionIssuedAt ?? now,
        };
    }

    public bool IsActive => !IsRevoked && ExpiresAt > DateTimeOffset.UtcNow;

    internal void Revoke() => IsRevoked = true;
}

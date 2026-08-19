namespace PawTrack.Domain.Auth;

/// <summary>Represents a revoked JWT access token's jti claim, stored so it cannot be reused.</summary>
public sealed class RevokedToken
{
    private RevokedToken() { } // EF Core

    public string Jti { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset RevokedAt { get; private set; }

    public static RevokedToken Create(string jti, DateTimeOffset expiresAt) => new()
    {
        Jti = jti,
        ExpiresAt = expiresAt,
        RevokedAt = DateTimeOffset.UtcNow,
    };
}

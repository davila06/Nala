using System.Security.Cryptography;
using System.Text;

namespace PawTrack.Domain.Auth;

public sealed class TrustedDevice
{
    private TrustedDevice() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid LastSessionId { get; private set; }
    public string DeviceName { get; private set; } = string.Empty;
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastUsedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsActive => !RevokedAt.HasValue && ExpiresAt > DateTimeOffset.UtcNow;

    public static (TrustedDevice Device, string RawToken) Create(
        Guid userId,
        Guid sessionId,
        string deviceName,
        TimeSpan lifetime)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId is required.", nameof(userId));
        if (sessionId == Guid.Empty) throw new ArgumentException("SessionId is required.", nameof(sessionId));
        if (string.IsNullOrWhiteSpace(deviceName)) throw new ArgumentException("Device name is required.", nameof(deviceName));
        if (lifetime <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(lifetime));

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var now = DateTimeOffset.UtcNow;
        var device = new TrustedDevice
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            LastSessionId = sessionId,
            DeviceName = deviceName.Trim(),
            TokenHash = Hash(rawToken),
            CreatedAt = now,
            LastUsedAt = now,
            ExpiresAt = now.Add(lifetime),
        };
        return (device, rawToken);
    }

    public void RotateToken(string rawToken, string tokenHash, Guid sessionId)
    {
        if (!IsActive) throw new InvalidOperationException("Trusted device is not active.");
        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(TokenHash),
                Convert.FromHexString(Hash(rawToken))))
            throw new InvalidOperationException("Trusted device proof is invalid.");
        if (string.IsNullOrWhiteSpace(tokenHash) || tokenHash.Length != 64)
            throw new ArgumentException("Token hash must be a SHA-256 hex digest.", nameof(tokenHash));
        if (sessionId == Guid.Empty) throw new ArgumentException("SessionId is required.", nameof(sessionId));

        TokenHash = tokenHash;
        LastSessionId = sessionId;
        LastUsedAt = DateTimeOffset.UtcNow;
    }

    public void Revoke() => RevokedAt ??= DateTimeOffset.UtcNow;

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}

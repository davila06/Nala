namespace PawTrack.Domain.Collars;

public sealed class CollarDeviceCredential
{
    private CollarDeviceCredential() { } // EF Core

    public Guid Id { get; private set; }
    public Guid CollarId { get; private set; }
    /// <summary>SHA-256 hex of the raw key — the raw key is never stored.</summary>
    public string KeyHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }

    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsUsable => !IsRevoked;

    public static CollarDeviceCredential Create(Guid collarId, string keyHash) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            CollarId = collarId,
            KeyHash = keyHash,
            CreatedAt = DateTimeOffset.UtcNow,
        };

    public void Revoke() => RevokedAt = DateTimeOffset.UtcNow;

    public void RecordUsage() => LastUsedAt = DateTimeOffset.UtcNow;
}

namespace PawTrack.Domain.Safety;

/// <summary>
/// A one-time 4-digit numeric code that the pet owner generates and verbally shares
/// with the rescuer at the moment of physical handover.  The rescuer types the code
/// into the app to confirm the safe delivery, closing the chain of custody.
/// </summary>
public sealed class HandoverCode
{
    private HandoverCode() { } // EF Core

    public Guid           Id               { get; private set; }
    public Guid           LostPetEventId   { get; private set; }

    /// <summary>4-digit numeric string (e.g. "4827"). Stored in plain text — codes
    /// are short-lived (24 h), single-use, and require authenticated access.</summary>
    public string         Code             { get; private set; } = string.Empty;

    public DateTimeOffset GeneratedAt      { get; private set; }
    public DateTimeOffset ExpiresAt        { get; private set; }

    public bool           IsUsed           { get; private set; }
    public DateTimeOffset? UsedAt          { get; private set; }
    public Guid?          VerifiedByUserId { get; private set; }

    // ── Factory ────────────────────────────────────────────────────────────────

    /// <summary>Generates a fresh 4-digit code expiring in 24 hours.</summary>
    public static HandoverCode Generate(Guid lostPetEventId)
    {
        // Use CSPRNG — System.Random is not cryptographically secure.
        var code = System.Security.Cryptography.RandomNumberGenerator
            .GetInt32(1000, 10000)
            .ToString("D4");
        var now  = DateTimeOffset.UtcNow;

        return new HandoverCode
        {
            Id             = Guid.CreateVersion7(),
            LostPetEventId = lostPetEventId,
            Code           = code,
            GeneratedAt    = now,
            ExpiresAt      = now.AddHours(24),
            IsUsed         = false,
        };
    }

    // ── Domain logic ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <c>true</c> when <paramref name="candidate"/> matches the stored code,
    /// the code has not been used, and it has not expired.
    /// </summary>
    public bool IsValid(string candidate) =>
        !IsUsed
        && DateTimeOffset.UtcNow <= ExpiresAt
        && string.Equals(candidate?.Trim(), Code, StringComparison.Ordinal);

    public void MarkAsUsed(Guid verifiedByUserId)
    {
        IsUsed           = true;
        UsedAt           = DateTimeOffset.UtcNow;
        VerifiedByUserId = verifiedByUserId;
    }
}

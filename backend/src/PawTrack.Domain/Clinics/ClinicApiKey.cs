namespace PawTrack.Domain.Clinics;

public sealed class ClinicApiKey
{
    private ClinicApiKey() { } // EF Core

    public Guid Id { get; private set; }
    public Guid ClinicId { get; private set; }
    /// <summary>SHA-256 hex of the raw key — never store the raw key.</summary>
    public string KeyHash { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public bool IsRevoked { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }
    /// <summary>Keys expire after 1 year by default — long-lived unrotated keys are a security risk.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }
    /// <summary>When set, this key was replaced by a rotation and should be treated as historical.</summary>
    public Guid? RotatedToKeyId { get; private set; }

    public static ClinicApiKey Create(Guid clinicId, string keyHash, string label, TimeSpan? lifetime = null) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            ClinicId = clinicId,
            KeyHash = keyHash,
            Label = label.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.Add(lifetime ?? TimeSpan.FromDays(365)),
        };

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsUsable => !IsRevoked && !IsExpired;

    public void Revoke() => IsRevoked = true;
    public void RecordUsage() => LastUsedAt = DateTimeOffset.UtcNow;
    public void MarkRotatedTo(Guid newKeyId)
    {
        RotatedToKeyId = newKeyId;
        Revoke();
    }
}


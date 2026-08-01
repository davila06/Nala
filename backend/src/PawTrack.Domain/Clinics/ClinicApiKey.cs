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

    public static ClinicApiKey Create(Guid clinicId, string keyHash, string label) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            ClinicId = clinicId,
            KeyHash = keyHash,
            Label = label.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };

    public void Revoke() => IsRevoked = true;
    public void RecordUsage() => LastUsedAt = DateTimeOffset.UtcNow;
}

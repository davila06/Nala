namespace PawTrack.Domain.Collars;

public sealed class Collar
{
    private Collar() { } // EF Core

    public Guid Id { get; private set; }
    public Guid PetId { get; private set; }
    public Guid OwnerId { get; private set; }
    public CollarProvider Provider { get; private set; }
    /// <summary>Provider-specific device identifier (Tractive tracker ID, etc.).</summary>
    public string? ExternalDeviceId { get; private set; }
    /// <summary>Encrypted OAuth access token for the provider API. Null for manual/generic.</summary>
    public string? ExternalTokenEncrypted { get; private set; }
    public int? BatteryPercent { get; private set; }
    public double? LastLat { get; private set; }
    public double? LastLng { get; private set; }
    public DateTimeOffset? LastSeenAt { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset RegisteredAt { get; private set; }

    // ── Factory ─────────────────────────────────────────────────────────────

    public static Collar Register(Guid petId, Guid ownerId, CollarProvider provider, string? externalDeviceId)
    {
        return new Collar
        {
            Id = Guid.CreateVersion7(),
            PetId = petId,
            OwnerId = ownerId,
            Provider = provider,
            ExternalDeviceId = externalDeviceId,
            IsActive = true,
            RegisteredAt = DateTimeOffset.UtcNow,
        };
    }

    // ── Behaviour ────────────────────────────────────────────────────────────

    public void UpdateLocation(double lat, double lng, int? batteryPercent)
    {
        LastLat = lat;
        LastLng = lng;
        BatteryPercent = batteryPercent;
        LastSeenAt = DateTimeOffset.UtcNow;
    }

    public void SetToken(string encryptedToken) => ExternalTokenEncrypted = encryptedToken;

    public void Deactivate() => IsActive = false;
}

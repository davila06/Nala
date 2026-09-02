using PawTrack.Domain.Common;

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
    /// <summary>Physical serial of the CollarTag device; null for third-party integrations.</summary>
    public string? CollarTagSerial { get; private set; }

    /// <summary>Whether the owner wants to be notified when this collar stops reporting.</summary>
    public bool OfflineAlertsEnabled { get; private set; } = true;
    /// <summary>Minutes without a location update before the collar is considered offline.</summary>
    public int OfflineThresholdMinutes { get; private set; } = 120;
    /// <summary>Set by <see cref="MarkOffline"/> / cleared by <see cref="UpdateLocation"/>.</summary>
    public bool IsOffline { get; private set; }
    /// <summary>Whether the owner wants to be notified when battery drops below the threshold.</summary>
    public bool BatteryAlertsEnabled { get; private set; } = true;
    /// <summary>Battery percentage below which a low-battery alert is triggered.</summary>
    public int BatteryAlertThresholdPercent { get; private set; } = 20;

    /// <summary>True while the owner has marked this collar's pet as actively being searched for.</summary>
    public bool IsLost { get; private set; }
    public DateTimeOffset? LostModeActivatedAt { get; private set; }
    /// <summary>The LostPetEvent this collar's live position feeds while in lost mode.</summary>
    public Guid? LostPetEventId { get; private set; }

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
            OfflineAlertsEnabled = true,
            OfflineThresholdMinutes = 120,
            BatteryAlertsEnabled = true,
            BatteryAlertThresholdPercent = 20,
        };
    }

    // ── Behaviour ────────────────────────────────────────────────────────────

    public void UpdateLocation(double lat, double lng, int? batteryPercent)
    {
        LastLat = lat;
        LastLng = lng;
        BatteryPercent = batteryPercent;
        LastSeenAt = DateTimeOffset.UtcNow;
        IsOffline = false; // any fresh report clears the offline flag
    }

    public void ActivateLostMode(Guid lostPetEventId)
    {
        IsLost = true;
        LostModeActivatedAt = DateTimeOffset.UtcNow;
        LostPetEventId = lostPetEventId;
    }

    public void DeactivateLostMode()
    {
        IsLost = false;
        LostModeActivatedAt = null;
        LostPetEventId = null;
    }

    public void SetToken(string encryptedToken) => ExternalTokenEncrypted = encryptedToken;

    public void SetTagSerial(string serial) => CollarTagSerial = serial;

    public void Deactivate() => IsActive = false;

    /// <summary>Marks the collar offline. Called by the connectivity detection job.</summary>
    public void MarkOffline() => IsOffline = true;

    public Result<bool> UpdateNotificationPreferences(
        bool offlineAlertsEnabled,
        int offlineThresholdMinutes,
        bool batteryAlertsEnabled,
        int batteryAlertThresholdPercent)
    {
        if (offlineThresholdMinutes < 15 || offlineThresholdMinutes > 1440)
            return Result.Failure<bool>("El umbral de desconexión debe estar entre 15 y 1440 minutos.");
        if (batteryAlertThresholdPercent < 5 || batteryAlertThresholdPercent > 50)
            return Result.Failure<bool>("El umbral de batería debe estar entre 5% y 50%.");

        OfflineAlertsEnabled = offlineAlertsEnabled;
        OfflineThresholdMinutes = offlineThresholdMinutes;
        BatteryAlertsEnabled = batteryAlertsEnabled;
        BatteryAlertThresholdPercent = batteryAlertThresholdPercent;
        return Result.Success(true);
    }
}

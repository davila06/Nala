using PawTrack.Domain.Collars;

namespace PawTrack.Application.Collars.DTOs;

public sealed record CollarDto(
    Guid Id,
    Guid PetId,
    CollarProvider Provider,
    string? ExternalDeviceId,
    int? BatteryPercent,
    double? LastLat,
    double? LastLng,
    DateTimeOffset? LastSeenAt,
    bool IsActive,
    string? CollarTagSerial,
    bool IsOffline,
    bool OfflineAlertsEnabled,
    int OfflineThresholdMinutes,
    bool BatteryAlertsEnabled,
    int BatteryAlertThresholdPercent,
    /// <summary>Device/provider timestamp of the last accepted fix \u2014 use this, not <see cref="LastSeenAt"/>, to judge freshness.</summary>
    DateTimeOffset? LastLocationRecordedAt,
    /// <summary>Connectivity flag reported by the provider at the last accepted fix.</summary>
    bool? LastPositionOnline,
    /// <summary>Seconds since the last accepted fix was recorded by the device/provider \u2014 the visible "age" of the position.</summary>
    double? PositionAgeSeconds)
{
    public static CollarDto FromDomain(Collar c) => FromDomain(c, DateTimeOffset.UtcNow);

    public static CollarDto FromDomain(Collar c, DateTimeOffset now) => new(
        c.Id, c.PetId, c.Provider, c.ExternalDeviceId,
        c.BatteryPercent, c.LastLat, c.LastLng, c.LastSeenAt, c.IsActive,
        c.CollarTagSerial, c.IsOffline, c.OfflineAlertsEnabled, c.OfflineThresholdMinutes,
        c.BatteryAlertsEnabled, c.BatteryAlertThresholdPercent,
        c.LastLocationRecordedAt, c.LastPositionOnline,
        c.LastLocationRecordedAt is { } recordedAt ? (now - recordedAt).TotalSeconds : null);
}

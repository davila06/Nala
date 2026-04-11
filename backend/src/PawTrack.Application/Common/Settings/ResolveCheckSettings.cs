namespace PawTrack.Application.Common.Settings;

/// <summary>
/// Parametrizable thresholds for the auto-resolve and stale-report flows.
/// All values can be overridden via appsettings.json under "ResolveCheck:*".
/// </summary>
public sealed class ResolveCheckSettings
{
    /// <summary>Days a report must be active before it is considered stale. Default: 30.</summary>
    public int StaleDays { get; init; } = 30;

    /// <summary>Days before a stale-report reminder can be re-sent to the same owner. Default: 7.</summary>
    public int ReminderCooldownDays { get; init; } = 7;

    /// <summary>Hours of QR-scan silence after which a ResolveCheck prompt is triggered. Default: 24.</summary>
    public int SilenceThresholdHours { get; init; } = 24;

    /// <summary>Metres from the owner's last known home location that trigger a proximity ResolveCheck on QR scan. Default: 200.</summary>
    public int HomeProximityMetres { get; init; } = 200;

    /// <summary>Metres from the owner's last known home location that trigger a proximity ResolveCheck on new sighting. Default: 100.</summary>
    public int SightingHomeProximityMetres { get; init; } = 100;

    /// <summary>Hours within which duplicate ResolveCheck notifications are suppressed. Default: 6.</summary>
    public int DedupWindowHours { get; init; } = 6;
}

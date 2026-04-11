namespace PawTrack.Application.Common.Settings;

/// <summary>
/// Configures QR-scan event retention. Records older than RetentionDays are purged nightly.
/// Override via appsettings.json under "QrScanRetention:*".
/// </summary>
public sealed class QrScanRetentionSettings
{
    /// <summary>Days to keep QR-scan events. Default: 90.</summary>
    public int RetentionDays { get; init; } = 90;
}

namespace PawTrack.Application.Common.Settings;

/// <summary>
/// Configures retention windows for personal data categories that, unlike collar
/// locations / QR scans / clinic views, have no natural short-term expiry: sightings,
/// closed chat threads, and read notifications. Reflects the Ley 8968 (Costa Rica)
/// proportional conservation principle (Art. 6/11) — data is not kept longer than
/// necessary once it no longer serves its original purpose.
/// Override via appsettings.json under "PersonalDataRetention:*".
/// </summary>
public sealed class PersonalDataRetentionSettings
{
    /// <summary>Days to keep sighting reports. Default: 730 (2 years).</summary>
    public int SightingRetentionDays { get; init; } = 730;

    /// <summary>Days to keep a chat thread after it is Closed. Default: 730 (2 years).</summary>
    public int ClosedChatRetentionDays { get; init; } = 730;

    /// <summary>Days to keep a notification after it has been read. Default: 365 (1 year).</summary>
    public int ReadNotificationRetentionDays { get; init; } = 365;
}

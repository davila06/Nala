namespace PawTrack.Domain.Locations;

/// <summary>
/// Stores the last known geographic location of a user and their opt-in preference
/// for receiving geofenced lost-pet alerts.
/// <para>
/// Invariants: each user has at most one <see cref="UserLocation"/> record (UserId = primary key).
/// </para>
/// </summary>
public sealed class UserLocation
{
    private UserLocation() { } // EF Core

    /// <summary>References <c>Auth.Users.Id</c>; serves as the primary key (one row per user).</summary>
    public Guid UserId { get; private set; }

    /// <summary>WGS-84 latitude in decimal degrees.</summary>
    public double Lat { get; private set; }

    /// <summary>WGS-84 longitude in decimal degrees.</summary>
    public double Lng { get; private set; }

    /// <summary>
    /// When <c>true</c> the user will receive a push notification when a pet is reported lost
    /// within <see cref="GeofenceConstants.DefaultAlertRadiusMetres"/> metres of this location.
    /// </summary>
    public bool ReceiveNearbyAlerts { get; private set; }

    /// <summary>UTC instant of the last location update.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    // ── Quiet hours ───────────────────────────────────────────────────────────

    /// <summary>
    /// Start of the daily quiet window in Costa Rica local time (UTC-6).
    /// <c>null</c> means no quiet window is configured.
    /// </summary>
    public TimeOnly? QuietHoursStart { get; private set; }

    /// <summary>
    /// End of the daily quiet window in Costa Rica local time (UTC-6).
    /// <c>null</c> means no quiet window is configured.
    /// </summary>
    public TimeOnly? QuietHoursEnd { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>Creates a new <see cref="UserLocation"/> record for first-time opt-in.</summary>
    public static UserLocation Create(
        Guid userId,
        double lat,
        double lng,
        bool receiveNearbyAlerts,
        TimeOnly? quietHoursStart = null,
        TimeOnly? quietHoursEnd   = null) =>
        new()
        {
            UserId = userId,
            Lat = lat,
            Lng = lng,
            ReceiveNearbyAlerts = receiveNearbyAlerts,
            QuietHoursStart = quietHoursStart,
            QuietHoursEnd   = quietHoursEnd,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    // ── Mutations ─────────────────────────────────────────────────────────────

    /// <summary>Updates coordinates and/or opt-in preference in place.</summary>
    public void Update(double lat, double lng, bool receiveNearbyAlerts)
    {
        Lat = lat;
        Lng = lng;
        ReceiveNearbyAlerts = receiveNearbyAlerts;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Replaces the quiet-hours window.  Pass <c>null</c> for both arguments to disable.
    /// </summary>
    public void SetQuietHours(TimeOnly? start, TimeOnly? end)
    {
        QuietHoursStart = start;
        QuietHoursEnd   = end;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // ── Domain logic ──────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <c>true</c> when <paramref name="utcNow"/> falls inside the user's configured
    /// quiet-hours window (evaluated in Costa Rica time, UTC-6).
    /// Returns <c>false</c> when no window is configured.
    /// </summary>
    public bool IsInQuietHours(DateTimeOffset utcNow)
    {
        if (QuietHoursStart is null || QuietHoursEnd is null) return false;

        // Costa Rica does not observe DST — UTC-6 year-round.
        var crTime = TimeOnly.FromTimeSpan(
            utcNow.ToOffset(TimeSpan.FromHours(-6)).TimeOfDay);

        var start = QuietHoursStart.Value;
        var end   = QuietHoursEnd.Value;

        return start <= end
            // Same-day window  (e.g. 08:00 → 20:00)
            ? crTime >= start && crTime < end
            // Overnight window (e.g. 23:00 → 07:00)
            : crTime >= start || crTime < end;
    }
}

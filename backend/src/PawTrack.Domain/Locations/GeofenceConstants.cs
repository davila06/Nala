namespace PawTrack.Domain.Locations;

/// <summary>
/// Domain constants for geofenced alert behaviour.
/// Centralised here so backend and tests share a single source of truth.
/// </summary>
public static class GeofenceConstants
{
    /// <summary>
    /// Radius (metres) used when querying users that should receive a geofenced lost-pet alert.
    /// Sources: PawTrack product spec § 9 — "radio de 1 km".
    /// </summary>
    public const int DefaultAlertRadiusMetres = 1_000;

    /// <summary>
    /// Minimum time (minutes) that must elapse before the same user can receive another
    /// geofenced lost-pet alert.  Prevents alert fatigue.
    /// Sources: PawTrack product spec § 9 — "máximo 1 por hora por usuario".
    /// </summary>
    public const int RateLimitWindowMinutes = 60;
}

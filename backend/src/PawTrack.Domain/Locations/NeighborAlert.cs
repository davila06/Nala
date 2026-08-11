namespace PawTrack.Domain.Locations;

/// <summary>
/// Opt-in record for a user who wants to receive ultra-local lost-pet alerts
/// (Guardia Vecinal). One record per user. Phone is stored as-entered — never
/// exposed publicly; used only for internal display and future OTP verification.
/// </summary>
public sealed class NeighborAlert
{
    private NeighborAlert() { }

    /// <summary>FK to Auth.Users; also serves as the primary key (one row per user).</summary>
    public Guid UserId { get; private set; }

    /// <summary>CR-format phone number (+506 or 8-digit). Stored as-entered; validated at input.</summary>
    public string Phone { get; private set; } = string.Empty;

    /// <summary>User's last-known latitude — sourced from UserLocation on enroll/update.</summary>
    public decimal Lat { get; private set; }

    /// <summary>User's last-known longitude — sourced from UserLocation on enroll/update.</summary>
    public decimal Lng { get; private set; }

    /// <summary>Alert radius in metres. Clamped to [100, 2000].</summary>
    public int RadiusMeters { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset EnrolledAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static NeighborAlert Enroll(Guid userId, string phone, decimal lat, decimal lng, int radiusMeters = 500)
    {
        return new NeighborAlert
        {
            UserId = userId,
            Phone = NormalizePhone(phone),
            Lat = lat,
            Lng = lng,
            RadiusMeters = Clamp(radiusMeters),
            IsActive = true,
            EnrolledAt = DateTimeOffset.UtcNow,
        };
    }

    // ── Behaviour ─────────────────────────────────────────────────────────────

    public void Activate()   { IsActive = true;  UpdatedAt = DateTimeOffset.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTimeOffset.UtcNow; }

    public void SetRadius(int meters) { RadiusMeters = Clamp(meters); UpdatedAt = DateTimeOffset.UtcNow; }
    public void UpdatePhone(string phone) { Phone = NormalizePhone(phone); UpdatedAt = DateTimeOffset.UtcNow; }
    public void UpdateLocation(decimal lat, decimal lng) { Lat = lat; Lng = lng; UpdatedAt = DateTimeOffset.UtcNow; }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static int Clamp(int meters) => Math.Clamp(meters, 100, 2000);

    private static string NormalizePhone(string phone)
    {
        var p = phone.Trim();
        // Normalise: if 8 digits with no prefix → add +506
        if (p.Length == 8 && p.All(char.IsDigit))
            return $"+506 {p[..4]}-{p[4..]}";
        return p;
    }
}

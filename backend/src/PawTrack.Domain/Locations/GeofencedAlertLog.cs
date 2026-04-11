namespace PawTrack.Domain.Locations;

/// <summary>
/// Permanent record that a geofenced alert was dispatched to a specific user for a specific
/// lost-pet event.  Used to prevent re-alerting the same user for the same case even after
/// the hourly rate-limit window has expired.
/// </summary>
public sealed class GeofencedAlertLog
{
    private GeofencedAlertLog() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>The user who received the alert.</summary>
    public Guid UserId { get; private set; }

    /// <summary>The lost-pet event the alert was issued for.</summary>
    public Guid LostPetEventId { get; private set; }

    /// <summary>UTC instant at which the alert was sent.</summary>
    public DateTimeOffset SentAt { get; private set; }

    public static GeofencedAlertLog Create(Guid userId, Guid lostPetEventId) =>
        new()
        {
            Id             = Guid.NewGuid(),
            UserId         = userId,
            LostPetEventId = lostPetEventId,
            SentAt         = DateTimeOffset.UtcNow,
        };
}

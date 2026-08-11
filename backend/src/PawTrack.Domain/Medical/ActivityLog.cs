namespace PawTrack.Domain.Medical;

public sealed class ActivityLog
{
    private ActivityLog() { }

    public Guid Id { get; private set; }
    public Guid PetId { get; private set; }
    public Guid OwnerId { get; private set; }
    public DateOnly Date { get; private set; }
    public ActivityType Type { get; private set; }
    /// <summary>Activity duration in minutes. Must be between 1 and 1440.</summary>
    public int DurationMinutes { get; private set; }
    /// <summary>Distance in metres (optional — computed from GPS or entered manually).</summary>
    public int? DistanceMeters { get; private set; }
    public string? Notes { get; private set; }
    public ActivitySource Source { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static ActivityLog Record(
        Guid petId,
        Guid ownerId,
        DateOnly date,
        ActivityType type,
        int durationMinutes,
        int? distanceMeters = null,
        string? notes = null,
        ActivitySource source = ActivitySource.Manual)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(durationMinutes);
        return new ActivityLog
        {
            Id = Guid.CreateVersion7(),
            PetId = petId,
            OwnerId = ownerId,
            Date = date,
            Type = type,
            DurationMinutes = Math.Clamp(durationMinutes, 1, 1440),
            DistanceMeters = distanceMeters,
            Notes = notes?.Trim(),
            Source = source,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}

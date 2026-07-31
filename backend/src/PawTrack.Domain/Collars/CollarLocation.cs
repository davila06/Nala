namespace PawTrack.Domain.Collars;

/// <summary>Write-heavy append-only GPS track points. Purge entries older than 30 days via a background job.</summary>
public sealed class CollarLocation
{
    private CollarLocation() { }

    public Guid          Id         { get; private set; }
    public Guid          CollarId   { get; private set; }
    public double        Lat        { get; private set; }
    public double        Lng        { get; private set; }
    public int?          Accuracy   { get; private set; } // metres
    public DateTimeOffset RecordedAt { get; private set; }

    public static CollarLocation Record(Guid collarId, double lat, double lng, int? accuracy = null)
    {
        return new CollarLocation
        {
            Id         = Guid.CreateVersion7(),
            CollarId   = collarId,
            Lat        = lat,
            Lng        = lng,
            Accuracy   = accuracy,
            RecordedAt = DateTimeOffset.UtcNow,
        };
    }
}

namespace PawTrack.Domain.Collars;

/// <summary>Write-heavy append-only GPS track points. Purge entries older than 30 days via a background job.</summary>
public sealed class CollarLocation
{
    private CollarLocation() { }

    public Guid Id { get; private set; }
    public Guid CollarId { get; private set; }
    public double Lat { get; private set; }
    public double Lng { get; private set; }
    public int? Accuracy { get; private set; } // metres

    /// <summary>When the device/provider actually sensed this fix — the authoritative point-in-time for freshness.</summary>
    public DateTimeOffset RecordedAt { get; private set; }

    /// <summary>When PawTrack ingested this point. Never used for freshness — only for latency diagnostics.</summary>
    public DateTimeOffset ReceivedAt { get; private set; }

    /// <summary>Connectivity state reported by the provider at the moment of this fix.</summary>
    public bool IsOnline { get; private set; }

    public static CollarLocation Record(
        Guid collarId,
        double lat,
        double lng,
        DateTimeOffset recordedAt,
        int? accuracy = null,
        bool isOnline = true,
        DateTimeOffset? receivedAt = null)
    {
        return new CollarLocation
        {
            Id = Guid.CreateVersion7(),
            CollarId = collarId,
            Lat = lat,
            Lng = lng,
            Accuracy = accuracy,
            RecordedAt = recordedAt,
            ReceivedAt = receivedAt ?? DateTimeOffset.UtcNow,
            IsOnline = isOnline,
        };
    }
}

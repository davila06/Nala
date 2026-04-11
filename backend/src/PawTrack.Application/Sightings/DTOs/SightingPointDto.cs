namespace PawTrack.Application.Sightings.DTOs;

/// <summary>
/// A single geo-timestamped point along a pet's sighting trail.
/// Used to render the movement polyline on the public map.
/// </summary>
public sealed record SightingPointDto(
    double Lat,
    double Lng,
    DateTimeOffset OccurredAt,
    /// <summary>Zero-based index in the chronological trail (0 = oldest, n-1 = most recent).</summary>
    int SequenceIndex);

namespace PawTrack.Application.Sightings.DTOs;

/// <summary>
/// Projected movement result for a lost-pet event.
/// Computed purely from existing <see cref="PawTrack.Domain.Sightings.Sighting"/> records —
/// no additional tables required.
/// </summary>
public sealed record MovementPredictionDto(
    /// <summary>True when there are at least 2 time-ordered sightings to compute a vector.</summary>
    bool HasEnoughData,

    /// <summary>Projected latitude of the pet's probable current position.</summary>
    double? ProjectedLat,

    /// <summary>Projected longitude of the pet's probable current position.</summary>
    double? ProjectedLng,

    /// <summary>
    /// Uncertainty radius in metres around the projected position.
    /// Grows with elapsed time using dispersion factors: &lt;2 h → 1.2×, 2–6 h → 1.8×, &gt;6 h → 2.5×.
    /// </summary>
    double? RadiusMeters,

    /// <summary>
    /// Estimated prediction confidence from 5 to 80 percent.
    /// Driven by the number of sightings and the elapsed time since the last one.
    /// </summary>
    int? ConfidencePercent,

    /// <summary>Chronologically ordered trail points (oldest → newest) for polyline rendering.</summary>
    IReadOnlyList<SightingPointDto> TrailPoints,

    /// <summary>Human-readable explanation in Spanish, suitable for display to end users.</summary>
    string ExplanationText);

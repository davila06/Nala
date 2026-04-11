namespace PawTrack.Application.Common.Interfaces;

/// <summary>
/// Converts a free-text location description into geographic coordinates.
/// </summary>
public interface IGeocodingService
{
    /// <summary>
    /// Attempts to geocode <paramref name="locationText"/>.
    /// Returns <c>(null, null)</c> if geocoding fails or confidence is too low.
    /// </summary>
    Task<(double? Latitude, double? Longitude)> GeocodeAsync(
        string locationText, CancellationToken ct = default);
}

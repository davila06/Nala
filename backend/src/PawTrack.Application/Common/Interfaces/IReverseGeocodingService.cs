namespace PawTrack.Application.Common.Interfaces;

/// <summary>
/// Resolves Costa Rica canton names from geographic coordinates.
/// </summary>
public interface IReverseGeocodingService
{
    /// <summary>
    /// Returns the canton/municipality name for the given coordinate or <c>null</c>
    /// when no reliable result is available.
    /// </summary>
    Task<string?> ResolveCantonAsync(double lat, double lng, CancellationToken ct = default);
}

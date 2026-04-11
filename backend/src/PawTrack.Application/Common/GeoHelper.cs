namespace PawTrack.Application.Common;

/// <summary>
/// Pure-math helper for geographic distance calculations.
/// Uses the Haversine formula with the WGS-84 mean Earth radius.
/// </summary>
public static class GeoHelper
{
    private const double EarthRadiusMetres = 6_371_000.0;

    /// <summary>
    /// Returns the great-circle distance in metres between two WGS-84 coordinates.
    /// </summary>
    public static double DistanceMetres(double lat1, double lng1, double lat2, double lng2)
    {
        var φ1 = ToRadians(lat1);
        var φ2 = ToRadians(lat2);
        var Δφ = ToRadians(lat2 - lat1);
        var Δλ = ToRadians(lng2 - lng1);

        var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2)
              + Math.Cos(φ1) * Math.Cos(φ2)
              * Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);

        return 2 * EarthRadiusMetres * Math.Asin(Math.Sqrt(a));
    }

    /// <summary>
    /// Returns the latitude and longitude deltas (in decimal degrees) that correspond to
    /// <paramref name="radiusMetres"/> around <paramref name="lat"/>.
    /// Use these to build a bounding box for an efficient SQL pre-filter.
    /// </summary>
    public static (double DeltaLat, double DeltaLng) BoundingBoxDelta(double lat, int radiusMetres)
    {
        var deltaLat = radiusMetres / 111_111.0;
        // Longitude degrees shrink toward the poles; compensate with cos(lat).
        var deltaLng = radiusMetres / (111_111.0 * Math.Cos(ToRadians(lat)));
        return (deltaLat, deltaLng);
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}

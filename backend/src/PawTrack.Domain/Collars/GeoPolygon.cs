namespace PawTrack.Domain.Collars;

/// <summary>
/// Point-in-polygon test via the standard ray-casting algorithm. Pure, side-effect-free —
/// shared by <see cref="CollarSafeZone"/> for geofence breach detection.
/// </summary>
public static class GeoPolygon
{
    public static bool Contains(IReadOnlyList<(double Lat, double Lng)> polygon, double lat, double lng)
    {
        if (polygon.Count < 3) return false;

        var inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            var (latI, lngI) = polygon[i];
            var (latJ, lngJ) = polygon[j];

            var intersects = ((latI > lat) != (latJ > lat))
                && (lng < (lngJ - lngI) * (lat - latI) / (latJ - latI) + lngI);

            if (intersects) inside = !inside;
        }

        return inside;
    }
}

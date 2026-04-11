using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.LostPets;

namespace PawTrack.Infrastructure.LostPets;

/// <summary>
/// Generates a <paramref name="gridSize"/> × <paramref name="gridSize"/> grid of
/// <see cref="SearchZone"/>s centred on a WGS-84 coordinate, each covering
/// <paramref name="cellSizeMetres"/> × <paramref name="cellSizeMetres"/> metres.
///
/// <para>
/// Coordinate conversion uses the standard flat-earth approximation valid for small areas
/// (≤ 20 km at CR latitudes):
/// <list type="bullet">
///   <item>1 degree latitude  ≈ 111 111 m</item>
///   <item>1 degree longitude ≈ 111 111 × cos(lat) m</item>
/// </list>
/// GeoJSON polygons use [longitude, latitude] ring order (RFC 7946).
/// </para>
/// </summary>
internal sealed class SearchZoneGenerator : ISearchZoneGenerator
{
    // Row labels A-Z (supports grids up to 26 rows)
    private static readonly char[] RowLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

    private const double MetresPerDegreeLat = 111_111.0;

    public IReadOnlyList<SearchZone> Generate(
        Guid lostPetEventId,
        double centerLat,
        double centerLng,
        int cellSizeMetres = 300,
        int gridSize = 7)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellSizeMetres);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(gridSize);

        var zones = new List<SearchZone>(gridSize * gridSize);

        // Degrees per cell side
        double cellLat = cellSizeMetres / MetresPerDegreeLat;
        double cellLng = cellSizeMetres / (MetresPerDegreeLat * Math.Cos(ToRadians(centerLat)));

        // Half-grid offset: centres the grid on centerLat/Lng
        double halfGrid = gridSize / 2.0;

        for (int row = 0; row < gridSize; row++)
        {
            for (int col = 0; col < gridSize; col++)
            {
                // South-west corner of this cell
                double lat0 = centerLat + (row - halfGrid + 0.5) * cellLat - cellLat / 2.0;
                double lng0 = centerLng + (col - halfGrid + 0.5) * cellLng - cellLng / 2.0;

                // North-east corner of this cell
                double lat1 = lat0 + cellLat;
                double lng1 = lng0 + cellLng;

                string label = BuildLabel(row, col, gridSize);
                string geoJson = BuildGeoJsonPolygon(lat0, lng0, lat1, lng1);

                zones.Add(SearchZone.Create(lostPetEventId, label, geoJson));
            }
        }

        return zones.AsReadOnly();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Builds an "A1" … "G7" style label. Row from top (north), column from left (west).</summary>
    private static string BuildLabel(int row, int col, int gridSize)
    {
        // Row: A at north (highest latitude index)
        char rowChar = RowLetters[gridSize - 1 - row];
        int colNum   = col + 1;
        return $"Zona {rowChar}{colNum}";
    }

    /// <summary>
    /// Builds a closed GeoJSON Polygon string from two corner coordinates.
    /// Ring is counter-clockwise (RFC 7946 exterior ring convention).
    /// Coordinates are [longitude, latitude].
    /// </summary>
    private static string BuildGeoJsonPolygon(double lat0, double lng0, double lat1, double lng1)
    {
        // Four corners: SW → SE → NE → NW → SW (CCW when viewed from north)
        return
            "{\"type\":\"Polygon\",\"coordinates\":[["
            + $"[{lng0:F6},{lat0:F6}],"   // SW
            + $"[{lng1:F6},{lat0:F6}],"   // SE
            + $"[{lng1:F6},{lat1:F6}],"   // NE
            + $"[{lng0:F6},{lat1:F6}],"   // NW
            + $"[{lng0:F6},{lat0:F6}]"    // close ring
            + "]]}";
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}

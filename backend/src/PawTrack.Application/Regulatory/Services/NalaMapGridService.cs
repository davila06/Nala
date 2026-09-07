namespace PawTrack.Application.Regulatory.Services;

public sealed record MapPoint(double Latitude, double Longitude, string Layer, string Canton);

public sealed record NalaMapCell(
    double Latitude,
    double Longitude,
    string Layer,
    string Canton,
    int Count,
    bool IsSuppressed);

public static class NalaMapGridService
{
    private const double GridSize = 0.01;

    public static IReadOnlyList<NalaMapCell> Aggregate(
        IEnumerable<MapPoint> points,
        double south,
        double north,
        double west,
        double east,
        int threshold)
    {
        if (south >= north) throw new ArgumentException("South must be below north.", nameof(south));
        if (west >= east) throw new ArgumentException("West must be below east.", nameof(west));
        ArgumentOutOfRangeException.ThrowIfLessThan(threshold, 1);

        return points
            .Where(point => point.Latitude >= south && point.Latitude <= north
                && point.Longitude >= west && point.Longitude <= east)
            .GroupBy(point => (
                Latitude: Math.Floor(point.Latitude / GridSize) * GridSize,
                Longitude: Math.Floor(point.Longitude / GridSize) * GridSize,
                point.Layer,
                point.Canton))
            .Select(group => new NalaMapCell(
                Math.Round(group.Key.Latitude + GridSize / 2, 3),
                Math.Round(group.Key.Longitude + GridSize / 2, 3),
                group.Key.Layer,
                group.Key.Canton,
                group.Count() < threshold ? 0 : group.Count(),
                group.Count() < threshold))
            .OrderBy(cell => cell.Canton)
            .ThenBy(cell => cell.Layer)
            .ToList();
    }
}

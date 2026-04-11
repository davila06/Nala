using PawTrack.Application.Sightings.DTOs;
using PawTrack.Domain.Sightings;

namespace PawTrack.Application.Sightings.Services;

/// <summary>
/// Pure-static helper that computes a predictive movement projection from an
/// ordered sequence of sightings.  All calculations are done in-process —
/// no I/O, no side-effects — making the class trivially unit-testable.
/// </summary>
internal static class MovementProjectionCalculator
{
    private const double EarthRadiusMeters = 6_371_000.0;
    private const double MetersPerDegreeLat = 111_111.0;

    // Minimum number of sightings required to derive a movement vector.
    private const int MinRequiredSightings = 2;

    // Minimum uncertainty circle so the projected zone is always visible on the map.
    private const double MinRadiusMeters = 100.0;

    // ── Public entry point ────────────────────────────────────────────────────

    /// <summary>
    /// Computes a movement prediction from <paramref name="sightingsAscending"/>,
    /// which must be sorted by <c>SightedAt</c> ascending (oldest first).
    /// </summary>
    public static MovementPredictionDto Calculate(IReadOnlyList<Sighting> sightingsAscending)
    {
        if (sightingsAscending.Count < MinRequiredSightings)
        {
            return NotEnoughData(
                sightingsAscending,
                "Se necesitan al menos 2 avistamientos para proyectar el movimiento.");
        }

        var velocitiesMs = new List<double>(sightingsAscending.Count - 1);
        var bearingsRad = new List<double>(sightingsAscending.Count - 1);

        for (int i = 1; i < sightingsAscending.Count; i++)
        {
            var from = sightingsAscending[i - 1];
            var to = sightingsAscending[i];

            double timeSec = (to.SightedAt - from.SightedAt).TotalSeconds;
            if (timeSec <= 0) continue;

            double distMeters = HaversineMeters(from.Lat, from.Lng, to.Lat, to.Lng);
            velocitiesMs.Add(distMeters / timeSec);
            bearingsRad.Add(Bearing(from.Lat, from.Lng, to.Lat, to.Lng));
        }

        if (velocitiesMs.Count == 0)
        {
            return NotEnoughData(
                sightingsAscending,
                "Los avistamientos tienen marcas de tiempo idénticas; no es posible calcular la velocidad.");
        }

        double avgVelocityMs = velocitiesMs.Average();
        double avgBearingRad = AverageBearing(bearingsRad);

        var lastSighting = sightingsAscending[^1];
        TimeSpan timeSinceLast = DateTimeOffset.UtcNow - lastSighting.SightedAt;
        double timeSinceLastHours = timeSinceLast.TotalHours;

        double projectedDistMeters = avgVelocityMs * timeSinceLast.TotalSeconds;

        // Convert projected distance to degree offsets
        double latOffsetDeg = (projectedDistMeters * Math.Cos(avgBearingRad)) / MetersPerDegreeLat;
        double metersPerDegreeLng = MetersPerDegreeLat * Math.Cos(lastSighting.Lat * Math.PI / 180.0);
        double lngOffsetDeg = (projectedDistMeters * Math.Sin(avgBearingRad))
            / (metersPerDegreeLng > 0 ? metersPerDegreeLng : 1.0);

        double projectedLat = Math.Clamp(lastSighting.Lat + latOffsetDeg, -90.0, 90.0);
        double projectedLng = Math.Clamp(lastSighting.Lng + lngOffsetDeg, -180.0, 180.0);

        // Uncertainty radius grows with elapsed time
        double dispersionFactor = timeSinceLastHours < 2.0 ? 1.2
            : timeSinceLastHours < 6.0 ? 1.8
            : 2.5;

        double radiusMeters = Math.Max(projectedDistMeters * dispersionFactor, MinRadiusMeters);

        // Confidence: more sightings = higher base, more elapsed time = lower confidence
        int baseConfidence = sightingsAscending.Count switch
        {
            2 => 40,
            3 or 4 => 60,
            5 or 6 => 70,
            _ => 80,
        };
        int timeDecay = timeSinceLastHours < 2.0 ? 0
            : timeSinceLastHours < 6.0 ? 10
            : 25;

        int confidencePercent = Math.Max(5, baseConfidence - timeDecay);

        string explanation = BuildExplanation(
            timeSinceLastHours, avgVelocityMs, avgBearingRad, confidencePercent);

        return new MovementPredictionDto(
            HasEnoughData: true,
            ProjectedLat: projectedLat,
            ProjectedLng: projectedLng,
            RadiusMeters: radiusMeters,
            ConfidencePercent: confidencePercent,
            TrailPoints: BuildTrailPoints(sightingsAscending),
            ExplanationText: explanation);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static MovementPredictionDto NotEnoughData(
        IReadOnlyList<Sighting> sightings,
        string explanation) => new(
            HasEnoughData: false,
            ProjectedLat: null,
            ProjectedLng: null,
            RadiusMeters: null,
            ConfidencePercent: null,
            TrailPoints: BuildTrailPoints(sightings),
            ExplanationText: explanation);

    private static IReadOnlyList<SightingPointDto> BuildTrailPoints(
        IReadOnlyList<Sighting> sightings) =>
        sightings
            .Select((s, i) => new SightingPointDto(s.Lat, s.Lng, s.SightedAt, i))
            .ToList()
            .AsReadOnly();

    /// <summary>Haversine distance between two lat/lng points in metres.</summary>
    private static double HaversineMeters(
        double lat1, double lng1, double lat2, double lng2)
    {
        double dLat = ToRad(lat2 - lat1);
        double dLng = ToRad(lng2 - lng1);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
            + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
            * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return EarthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    /// <summary>
    /// Computes the initial bearing (in radians) from point 1 to point 2
    /// using the forward azimuth formula.
    /// </summary>
    private static double Bearing(
        double lat1, double lng1, double lat2, double lng2)
    {
        double lat1Rad = ToRad(lat1);
        double lat2Rad = ToRad(lat2);
        double dLng = ToRad(lng2 - lng1);

        double y = Math.Sin(dLng) * Math.Cos(lat2Rad);
        double x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad)
            - Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(dLng);
        return Math.Atan2(y, x);
    }

    /// <summary>
    /// Averages a list of bearings correctly by decomposing to unit vectors.
    /// Handles the 359°→1° wrap-around case.
    /// </summary>
    private static double AverageBearing(IReadOnlyList<double> bearings)
    {
        double sinSum = bearings.Sum(Math.Sin);
        double cosSum = bearings.Sum(Math.Cos);
        return Math.Atan2(sinSum / bearings.Count, cosSum / bearings.Count);
    }

    private static string BuildExplanation(
        double timeSinceLastHours,
        double avgVelocityMs,
        double avgBearingRad,
        int confidencePercent)
    {
        int hoursAgo = (int)Math.Floor(timeSinceLastHours);
        int minutesAgo = (int)((timeSinceLastHours - hoursAgo) * 60);

        string timeAgoText = hoursAgo > 0
            ? $"hace {hoursAgo}h {minutesAgo}min"
            : $"hace {minutesAgo}min";

        double bearingDeg = avgBearingRad * 180.0 / Math.PI;
        string direction = DegreesToSpanishCardinal(bearingDeg);
        double speedKmh = avgVelocityMs * 3.6;

        return $"Último avistamiento {timeAgoText}. " +
               $"Moviéndose hacia el {direction} a {speedKmh:F1} km/h aprox. " +
               $"Confianza: {confidencePercent}%.";
    }

    private static string DegreesToSpanishCardinal(double degrees)
    {
        // Normalize to [0, 360)
        degrees = ((degrees % 360.0) + 360.0) % 360.0;

        return degrees switch
        {
            < 22.5 => "norte",
            < 67.5 => "noreste",
            < 112.5 => "este",
            < 157.5 => "sureste",
            < 202.5 => "sur",
            < 247.5 => "suroeste",
            < 292.5 => "oeste",
            < 337.5 => "noroeste",
            _ => "norte",
        };
    }

    private static double ToRad(double degrees) => degrees * Math.PI / 180.0;
}

namespace PawTrack.Application.Sightings.VisualMatch;

/// <summary>
/// Pure math helpers for visual similarity matching. Zero-allocation inner loops via Span&lt;T&gt;.
/// </summary>
internal static class VectorMath
{
    /// <summary>
    /// Computes cosine similarity between two equal-length float vectors.
    /// Returns a value in [-1, 1]; returns 0 on empty or length-mismatch inputs.
    /// </summary>
    public static float CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length != b.Length || a.Length == 0) return 0f;

        float dot = 0f, normA = 0f, normB = 0f;
        for (var i = 0; i < a.Length; i++)
        {
            dot   += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        var divisor = MathF.Sqrt(normA) * MathF.Sqrt(normB);
        return divisor < 1e-9f ? 0f : dot / divisor;
    }

    /// <summary>
    /// Returns a geo-proximity score in [0, 1].
    /// Full score (1.0) at 0 km; decays linearly to 0 at <paramref name="decayKm"/> km.
    /// Returns 0.5 (neutral) when either location is absent.
    /// </summary>
    public static float GeoProximityScore(
        double? probeLatOpt, double? probeLngOpt,
        double? petLatOpt,   double? petLngOpt,
        float decayKm = 50f)
    {
        if (probeLatOpt is null || probeLngOpt is null ||
            petLatOpt is null   || petLngOpt   is null)
            return 0.5f;

        var distKm = HaversineKm(
            probeLatOpt.Value, probeLngOpt.Value,
            petLatOpt.Value,   petLngOpt.Value);

        return MathF.Max(0f, 1f - (float)distKm / decayKm);
    }

    /// <summary>Haversine great-circle distance in kilometres.</summary>
    public static double HaversineKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6_371.0;
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLng = (lng2 - lng1) * Math.PI / 180.0;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(lat1 * Math.PI / 180.0)
              * Math.Cos(lat2 * Math.PI / 180.0)
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return 2 * R * Math.Asin(Math.Sqrt(a));
    }
}

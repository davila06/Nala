using System.Text.Json;

namespace PawTrack.Domain.Collars;

public enum SafeZoneTransition
{
    /// <summary>No breach state change (still inside, still outside, or first-ever fix establishing a baseline).</summary>
    NoChange,
    /// <summary>Collar was inside the zone and is now outside — triggers an alert.</summary>
    Breached,
    /// <summary>Collar was outside the zone and has returned inside.</summary>
    Returned,
}

/// <summary>
/// A virtual fence around a collar. Points are stored as a JSON array of
/// <c>{"lat":..,"lng":..}</c> objects (simpler than full GeoJSON, but plays nicely
/// with Leaflet's <c>getLatLngs()</c> output already used elsewhere in the app).
/// </summary>
public sealed class CollarSafeZone
{
    private CollarSafeZone() { } // EF Core

    public Guid Id { get; private set; }
    public Guid CollarId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string PolygonJson { get; private set; } = string.Empty;
    public bool Enabled { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Null until the first location fix is evaluated against this zone.</summary>
    public bool? LastKnownInside { get; private set; }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static CollarSafeZone Create(Guid collarId, string name, string polygonJson)
    {
        var points = ParsePoints(polygonJson); // throws ArgumentException if invalid
        if (points.Count < 3)
            throw new ArgumentException("Una zona segura requiere al menos 3 puntos.");

        return new CollarSafeZone
        {
            Id = Guid.CreateVersion7(),
            CollarId = collarId,
            Name = name.Trim(),
            PolygonJson = polygonJson,
            Enabled = true,
            CreatedAt = DateTimeOffset.UtcNow,
            LastKnownInside = null,
        };
    }

    public void Update(string name, string polygonJson, bool enabled)
    {
        var points = ParsePoints(polygonJson);
        if (points.Count < 3)
            throw new ArgumentException("Una zona segura requiere al menos 3 puntos.");

        Name = name.Trim();
        PolygonJson = polygonJson;
        Enabled = enabled;
        LastKnownInside = null; // polygon changed — re-establish baseline on next fix
    }

    /// <summary>Evaluates a new position against this zone and updates the tracked breach state.</summary>
    public SafeZoneTransition Evaluate(double lat, double lng)
    {
        var isInside = GeoPolygon.Contains(ParsePoints(PolygonJson), lat, lng);

        var transition = LastKnownInside switch
        {
            null => SafeZoneTransition.NoChange,
            true when !isInside => SafeZoneTransition.Breached,
            false when isInside => SafeZoneTransition.Returned,
            _ => SafeZoneTransition.NoChange,
        };

        LastKnownInside = isInside;
        return transition;
    }

    private static List<(double Lat, double Lng)> ParsePoints(string polygonJson)
    {
        try
        {
            var raw = JsonSerializer.Deserialize<List<PolygonPointDto>>(polygonJson, JsonOptions)
                ?? throw new ArgumentException("Polígono inválido.");
            return raw.Select(p => (p.Lat, p.Lng)).ToList();
        }
        catch (JsonException)
        {
            throw new ArgumentException("El formato del polígono no es un JSON válido.");
        }
    }

    private sealed record PolygonPointDto(double Lat, double Lng);
}

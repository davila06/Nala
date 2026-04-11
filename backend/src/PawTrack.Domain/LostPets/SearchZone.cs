namespace PawTrack.Domain.LostPets;

/// <summary>
/// A geographic zone generated as part of a coordinated field search for a lost pet.
/// Each zone covers approximately 300×300 metres and holds a GeoJSON polygon
/// that can be rendered on a map.
/// </summary>
public sealed class SearchZone
{
    private SearchZone() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>Reference to the lost-pet event this zone belongs to.</summary>
    public Guid LostPetEventId { get; private set; }

    /// <summary>Human-readable label, e.g. "Zona B3 - San Pedro".</summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>GeoJSON Polygon string (RFC 7946). Uses [lng, lat] coordinate order.</summary>
    public string GeoJsonPolygon { get; private set; } = string.Empty;

    public SearchZoneStatus Status { get; private set; }

    /// <summary>ID of the user who has claimed or cleared this zone. Null when <see cref="Status"/> is <see cref="SearchZoneStatus.Free"/>.</summary>
    public Guid? AssignedToUserId { get; private set; }

    public DateTimeOffset? TakenAt { get; private set; }

    public DateTimeOffset? ClearedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Factory used by <c>SearchZoneGenerator</c> during zone grid creation.</summary>
    public static SearchZone Create(Guid lostPetEventId, string label, string geoJsonPolygon) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            LostPetEventId = lostPetEventId,
            Label = label.Trim(),
            GeoJsonPolygon = geoJsonPolygon,
            Status = SearchZoneStatus.Free,
            AssignedToUserId = null,
            TakenAt = null,
            ClearedAt = null,
            CreatedAt = DateTimeOffset.UtcNow,
        };

    // ── State transitions ─────────────────────────────────────────────────────

    /// <summary>Claim this zone for <paramref name="userId"/>. Only valid when <see cref="Status"/> is <see cref="SearchZoneStatus.Free"/>.</summary>
    public bool TryClaim(Guid userId)
    {
        if (Status != SearchZoneStatus.Free) return false;

        Status = SearchZoneStatus.Taken;
        AssignedToUserId = userId;
        TakenAt = DateTimeOffset.UtcNow;
        ClearedAt = null;
        return true;
    }

    /// <summary>Mark zone as cleared. Only valid when <see cref="Status"/> is <see cref="SearchZoneStatus.Taken"/> and caller is the assigned user.</summary>
    public bool TryClear(Guid userId)
    {
        if (Status != SearchZoneStatus.Taken) return false;
        if (AssignedToUserId != userId) return false;

        Status = SearchZoneStatus.Clear;
        ClearedAt = DateTimeOffset.UtcNow;
        return true;
    }

    /// <summary>Release this zone back to <see cref="SearchZoneStatus.Free"/>. Only valid when taken by <paramref name="userId"/>.</summary>
    public bool TryRelease(Guid userId)
    {
        if (Status != SearchZoneStatus.Taken) return false;
        if (AssignedToUserId != userId) return false;

        Status = SearchZoneStatus.Free;
        AssignedToUserId = null;
        TakenAt = null;
        return true;
    }
}

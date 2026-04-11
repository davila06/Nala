using PawTrack.Domain.LostPets;

namespace PawTrack.Application.LostPets.DTOs;

/// <summary>
/// Read-only representation of a <see cref="SearchZone"/> for API consumers.
/// <para>
/// <b>Privacy note:</b> <c>AssignedToUserId</c> is intentionally NOT exposed —
/// returning the raw GUID would allow any authenticated user to build a
/// cross-event profile of volunteer activity (who searches, how often, patterns).
/// Use <see cref="IsAssigned"/> to render a zone's availability status; the
/// frontend derives "IsAssignedToMe" from its own local claim state.
/// </para>
/// </summary>
public sealed record SearchZoneDto(
    Guid Id,
    Guid LostPetEventId,
    string Label,
    string GeoJsonPolygon,
    string Status,
    /// <summary><c>true</c> when the zone is currently claimed by any volunteer.</summary>
    bool IsAssigned,
    DateTimeOffset? TakenAt,
    DateTimeOffset? ClearedAt)
{
    public static SearchZoneDto FromDomain(SearchZone zone) =>
        new(
            zone.Id,
            zone.LostPetEventId,
            zone.Label,
            zone.GeoJsonPolygon,
            zone.Status.ToString(),
            zone.AssignedToUserId.HasValue && zone.Status == SearchZoneStatus.Taken,
            zone.TakenAt,
            zone.ClearedAt);
}

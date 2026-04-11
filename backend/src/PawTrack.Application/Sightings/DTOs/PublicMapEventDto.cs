using PawTrack.Domain.LostPets;
using PawTrack.Domain.Sightings;

namespace PawTrack.Application.Sightings.DTOs;

/// <summary>
/// Lightweight DTO for the public map endpoint.
/// Contains only the information needed to render a marker on the map.
/// </summary>
public sealed record PublicMapEventDto(
    string Id,
    /// <summary>"LostPet" | "Sighting"</summary>
    string EventType,
    string PetId,
    double Lat,
    double Lng,
    string? PhotoUrl,
    DateTimeOffset OccurredAt)
{
    public static PublicMapEventDto FromSighting(Sighting s) => new(
        s.Id.ToString(),
        "Sighting",
        s.PetId.ToString(),
        s.Lat,
        s.Lng,
        s.PhotoUrl,
        s.SightedAt);

    public static PublicMapEventDto FromLostPet(LostPetEvent lpe) => new(
        lpe.Id.ToString(),
        "LostPet",
        lpe.PetId.ToString(),
        lpe.LastSeenLat ?? 0,
        lpe.LastSeenLng ?? 0,
        null,
        lpe.ReportedAt);
}

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
    /// <summary>Display name of the pet. Null when the pet record was not found.</summary>
    string? PetName,
    /// <summary>Species as a lowercase string, e.g. "dog", "cat". Null when not found.</summary>
    string? Species,
    double Lat,
    double Lng,
    string? PhotoUrl,
    DateTimeOffset OccurredAt)
{
    public static PublicMapEventDto FromSighting(Sighting s, string? petName, string? species) => new(
        s.Id.ToString(),
        "Sighting",
        s.PetId.ToString(),
        petName,
        species,
        s.Lat,
        s.Lng,
        s.PhotoUrl,
        s.SightedAt);

    public static PublicMapEventDto FromLostPet(LostPetEvent lpe, string? petName, string? species) => new(
        lpe.Id.ToString(),
        "LostPet",
        lpe.PetId.ToString(),
        petName,
        species,
        lpe.LastSeenLat ?? 0,
        lpe.LastSeenLng ?? 0,
        null,
        lpe.ReportedAt);
}

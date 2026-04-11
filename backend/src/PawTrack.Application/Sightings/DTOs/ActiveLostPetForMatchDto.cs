using PawTrack.Domain.Pets;

namespace PawTrack.Application.Sightings.DTOs;

/// <summary>
/// Lightweight projection of a LostPetEvent joined with its Pet, used for
/// found-pet match scoring. Read-only; never persisted.
/// </summary>
public sealed record ActiveLostPetForMatchDto(
    Guid LostPetEventId,
    Guid PetId,
    Guid OwnerId,
    string PetName,
    PetSpecies Species,
    string? PetPhotoUrl,
    double? LastSeenLat,
    double? LastSeenLng,
    DateTimeOffset ReportedAt);

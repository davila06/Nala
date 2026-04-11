using PawTrack.Domain.Pets;

namespace PawTrack.Application.Pets.DTOs;

/// <summary>Lightweight summary for dashboard list view.</summary>
public sealed record PetSummaryDto(
    string Id,
    string Name,
    string Species,
    string? Breed,
    string? PhotoUrl,
    string Status)
{
    public static PetSummaryDto FromDomain(Pet pet) => new(
        pet.Id.ToString(),
        pet.Name,
        pet.Species.ToString(),
        pet.Breed,
        pet.PhotoUrl,
        pet.Status.ToString());
}

using PawTrack.Domain.Pets;

namespace PawTrack.Application.Pets.DTOs;

/// <summary>Full pet data for authenticated owner view.</summary>
public sealed record PetDto(
    string Id,
    string OwnerId,
    string Name,
    string Species,
    string? Breed,
    string? BirthDate,
    string? PhotoUrl,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static PetDto FromDomain(Pet pet) => new(
        pet.Id.ToString(),
        pet.OwnerId.ToString(),
        pet.Name,
        pet.Species.ToString(),
        pet.Breed,
        pet.BirthDate?.ToString("yyyy-MM-dd"),
        pet.PhotoUrl,
        pet.Status.ToString(),
        pet.CreatedAt,
        pet.UpdatedAt);
}

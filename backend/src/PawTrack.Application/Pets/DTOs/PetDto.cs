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
    string? MicrochipId,
    string Sex,
    string? Color,
    string? DistinctiveMarks,
    string SterilizedStatus,
    string? SterilizedAt,
    string? ResidenceCanton,
    string MicrochipVerificationStatus,
    string? MicrochipVerifiedAt,
    string? MicrochipVerifiedByClinicId,
    string? MicrochipVerificationNotes,
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
        pet.MicrochipId,
        pet.Sex.ToString(),
        pet.Color,
        pet.DistinctiveMarks,
        pet.SterilizedStatus.ToString(),
        pet.SterilizedAt?.ToString("yyyy-MM-dd"),
        pet.ResidenceCanton,
        pet.MicrochipVerificationStatus.ToString(),
        pet.MicrochipVerifiedAt?.ToString("O"),
        pet.MicrochipVerifiedByClinicId?.ToString(),
        pet.MicrochipVerificationNotes,
        pet.CreatedAt,
        pet.UpdatedAt);
}

using PawTrack.Domain.Municipalities;

namespace PawTrack.Application.Municipalities.DTOs;

public sealed record CapturedAnimalDto(
    Guid                 Id,
    string               Canton,
    string               Species,
    string?              Breed,
    string               Color,
    string?              EstimatedAge,
    string?              PhotoUrl,
    string?              Notes,
    string?              CollarChipNumber,
    Guid?                MatchedPetId,
    CapturedAnimalStatus Status,
    DateTimeOffset       CapturedAt)
{
    public static CapturedAnimalDto FromDomain(CapturedAnimal a) => new(
        a.Id, a.Canton, a.Species, a.Breed, a.Color,
        a.EstimatedAge, a.PhotoUrl, a.Notes, a.CollarChipNumber,
        a.MatchedPetId, a.Status, a.CapturedAt);
}

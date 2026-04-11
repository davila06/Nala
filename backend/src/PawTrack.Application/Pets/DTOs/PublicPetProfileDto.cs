using PawTrack.Domain.LostPets;
using PawTrack.Domain.Pets;

namespace PawTrack.Application.Pets.DTOs;

/// <summary>Public pet profile — no owner PII exposed.</summary>
public sealed record PublicPetProfileDto(
    string Id,
    string Name,
    string Species,
    string? Breed,
    string? BirthDate,
    string? PhotoUrl,
    string Status,
    /// <summary>Owner user ID — opaque GUID, safe to expose publicly; required to open a chat thread.</summary>
    string OwnerId,
    /// <summary>ID of the current active lost-pet report, if any. Safe to expose publicly (UUID).</summary>
    string? ActiveLostEventId,
    /// <summary>Contact name provided at report time. Safe to expose publicly (not PII).</summary>
    string? ContactName,
    /// <summary>
    /// Owner's custom message displayed in the public QR profile banner when the pet is lost.
    /// Maximum 200 characters. Safe to expose publicly.
    /// </summary>
    string? PublicMessage)
{
    public static PublicPetProfileDto FromDomain(Pet pet, LostPetEvent? activeLostEvent = null) => new(
        pet.Id.ToString(),
        pet.Name,
        pet.Species.ToString(),
        pet.Breed,
        pet.BirthDate?.ToString("yyyy-MM-dd"),
        pet.PhotoUrl,
        pet.Status.ToString(),
        pet.OwnerId.ToString(),
        activeLostEvent?.Id.ToString(),
        activeLostEvent?.ContactName,
        activeLostEvent?.PublicMessage);
}

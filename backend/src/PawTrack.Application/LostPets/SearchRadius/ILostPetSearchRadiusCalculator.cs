using PawTrack.Domain.Pets;

namespace PawTrack.Application.LostPets.SearchRadius;

public interface ILostPetSearchRadiusCalculator
{
    int Calculate(
        PetSpecies species,
        string? breed,
        DateTimeOffset lastSeenAt,
        DateTimeOffset? referenceTime = null);
}
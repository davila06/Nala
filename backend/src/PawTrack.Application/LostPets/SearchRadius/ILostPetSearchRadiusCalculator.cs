using PawTrack.Domain.Pets;

namespace PawTrack.Application.LostPets.SearchRadius;

public interface ILostPetSearchRadiusCalculator
{
    /// <param name="tierMultiplier">1.0 = free, 3.33 = Plus, -1 = Familia (no cap). Pass 1.0 for default.</param>
    int Calculate(
        PetSpecies species,
        string? breed,
        DateTimeOffset lastSeenAt,
        double tierMultiplier = 1.0,
        DateTimeOffset? referenceTime = null);
}
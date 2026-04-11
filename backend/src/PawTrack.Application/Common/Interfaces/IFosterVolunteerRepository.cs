using PawTrack.Domain.Fosters;
using PawTrack.Domain.Pets;

namespace PawTrack.Application.Common.Interfaces;

public interface IFosterVolunteerRepository
{
    Task<FosterVolunteer?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(FosterVolunteer volunteer, CancellationToken cancellationToken = default);

    void Update(FosterVolunteer volunteer);

    Task<IReadOnlyList<FosterVolunteerSuggestion>> GetNearbyAvailableAsync(
        double lat,
        double lng,
        PetSpecies foundSpecies,
        int radiusMetres,
        CancellationToken cancellationToken = default);
}

public sealed record FosterVolunteerSuggestion(
    Guid UserId,
    string VolunteerName,
    double DistanceMetres,
    bool SpeciesMatch,
    string? SizePreference,
    int MaxDays);

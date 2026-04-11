using PawTrack.Domain.LostPets;
using PawTrack.Application.Sightings.DTOs;

namespace PawTrack.Application.Common.Interfaces;

public interface ILostPetRepository
{
    Task<LostPetEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LostPetEvent?> GetActiveByPetIdAsync(Guid petId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all <b>active</b> lost pet reports whose last-seen coordinates
    /// fall within the given bounding box.
    /// Reports without coordinates are excluded.
    /// </summary>
    Task<IReadOnlyList<LostPetEvent>> GetActiveLostPetsInBBoxAsync(
        double north,
        double south,
        double east,
        double west,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LostPetEvent>> GetActiveReportedBeforeAsync(
        DateTimeOffset reportedBefore,
        CancellationToken cancellationToken = default);

    Task AddAsync(LostPetEvent lostPetEvent, CancellationToken cancellationToken = default);
    void Update(LostPetEvent lostPetEvent);

    /// <summary>
    /// Returns active lost-pet events whose last-seen coordinates fall within the
    /// given bounding box, joined with the corresponding pet data for match scoring.
    /// </summary>
    Task<IReadOnlyList<ActiveLostPetForMatchDto>> GetActiveLostPetsForMatchAsync(
        double north,
        double south,
        double east,
        double west,
        CancellationToken cancellationToken = default);
}

using PawTrack.Application.Adoptions;
using PawTrack.Domain.Adoptions;
using PawTrack.Domain.Pets;

namespace PawTrack.Application.Common.Interfaces;

public interface IAdoptionRepository
{
    // ── Animals ───────────────────────────────────────────────────────────────

    Task<AdoptablePet?> GetAnimalByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AdoptablePet>> GetByOrganizationAsync(Guid orgUserId, int skip, int take, CancellationToken ct = default);
    Task<int> CountByOrganizationAsync(Guid orgUserId, CancellationToken ct = default);
    Task<(IReadOnlyList<AdoptablePet> Items, int Total)> GetAvailablePagedAsync(
        PetSpecies? species,
        PetSize? size,
        AgeCategory? ageCategory,
        bool? isVaccinated,
        bool? isSterilized,
        bool? okWithKids,
        bool? okWithDogs,
        double? nearLat,
        double? nearLng,
        int radiusKm,
        int skip,
        int take,
        CancellationToken ct = default);
    /// <summary>Returns all available animals for the map view; hard-capped at 500.</summary>
    Task<IReadOnlyList<AdoptablePet>> GetAvailableAllAsync(CancellationToken ct = default);
    Task<AdoptionAdminStatsDto> GetAdminStatsAsync(CancellationToken ct = default);
    Task<(IReadOnlyList<AdoptablePet> Items, int Total)> GetAllAdminPagedAsync(
        AdoptionStatus? status, int skip, int take, CancellationToken ct = default);
    Task AddAnimalAsync(AdoptablePet animal, CancellationToken ct = default);
    void UpdateAnimal(AdoptablePet animal);

    // ── Applications ──────────────────────────────────────────────────────────

    Task<AdoptionApplication?> GetApplicationByIdAsync(Guid id, CancellationToken ct = default);
    Task<AdoptionApplication?> GetApplicationByApplicantAndAnimalAsync(Guid applicantUserId, Guid animalId, CancellationToken ct = default);
    Task<IReadOnlyList<AdoptionApplication>> GetApplicationsByAnimalAsync(Guid animalId, CancellationToken ct = default);
    Task<IReadOnlyList<AdoptionApplication>> GetApplicationsByApplicantAsync(Guid applicantUserId, int skip, int take, CancellationToken ct = default);
    Task<int> CountApplicationsByApplicantAsync(Guid applicantUserId, CancellationToken ct = default);
    Task AddApplicationAsync(AdoptionApplication application, CancellationToken ct = default);
    void UpdateApplication(AdoptionApplication application);

    // ── Fairs ─────────────────────────────────────────────────────────────────

    Task<AdoptionFair?> GetFairByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AdoptionFair>> GetUpcomingFairsAsync(double? nearLat, double? nearLng, int radiusKm, CancellationToken ct = default);
    Task AddFairAsync(AdoptionFair fair, CancellationToken ct = default);
    void UpdateFair(AdoptionFair fair);
}

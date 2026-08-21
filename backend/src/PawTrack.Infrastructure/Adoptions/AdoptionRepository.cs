using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Adoptions;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Adoptions;
using PawTrack.Domain.Pets;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Adoptions;

public sealed class AdoptionRepository(PawTrackDbContext db) : IAdoptionRepository
{
    // ── Animals ───────────────────────────────────────────────────────────────

    public Task<AdoptablePet?> GetAnimalByIdAsync(Guid id, CancellationToken ct = default) =>
        db.AdoptableAnimals.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<AdoptablePet>> GetByOrganizationAsync(
        Guid orgUserId, int skip, int take, CancellationToken ct = default) =>
        await db.AdoptableAnimals.AsNoTracking()
            .Where(a => a.OrganizationUserId == orgUserId)
            .OrderByDescending(a => a.PublishedAt)
            .Skip(skip).Take(take)
            .ToListAsync(ct);

    public Task<int> CountByOrganizationAsync(Guid orgUserId, CancellationToken ct = default) =>
        db.AdoptableAnimals.CountAsync(a => a.OrganizationUserId == orgUserId, ct);

    public async Task<(IReadOnlyList<AdoptablePet> Items, int Total)> GetAvailablePagedAsync(
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
        CancellationToken ct = default)
    {
        var q = db.AdoptableAnimals.AsNoTracking()
            .Where(a => a.Status == AdoptionStatus.Available);

        if (species.HasValue) q = q.Where(a => a.Species == species.Value);
        if (size.HasValue) q = q.Where(a => a.Size == size.Value);
        if (ageCategory.HasValue) q = q.Where(a => a.AgeCategory == ageCategory.Value);
        if (isVaccinated == true) q = q.Where(a => a.IsVaccinated);
        if (isSterilized == true) q = q.Where(a => a.IsSterilized);
        if (okWithKids == true) q = q.Where(a => a.OkWithKids);
        if (okWithDogs == true) q = q.Where(a => a.OkWithDogs);

        // Bounding-box approximation (Haversine-precise at application layer if needed)
        if (nearLat.HasValue && nearLng.HasValue)
        {
            var latDelta = radiusKm / 111.0;
            var lngDelta = radiusKm / (111.0 * Math.Cos(nearLat.Value * Math.PI / 180.0));
            q = q.Where(a =>
                (double)a.RefLat >= nearLat.Value - latDelta && (double)a.RefLat <= nearLat.Value + latDelta &&
                (double)a.RefLng >= nearLng.Value - lngDelta && (double)a.RefLng <= nearLng.Value + lngDelta);
        }

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(a => a.PublishedAt).Skip(skip).Take(take).ToListAsync(ct);
        return (items, total);
    }

    public async Task<IReadOnlyList<AdoptablePet>> GetAvailableAllAsync(CancellationToken ct = default) =>
        await db.AdoptableAnimals.AsNoTracking()
            .Where(a => a.Status == AdoptionStatus.Available)
            .OrderByDescending(a => a.PublishedAt)
            .Take(500) // hard cap for map pins
            .ToListAsync(ct);

    public async Task<AdoptionAdminStatsDto> GetAdminStatsAsync(CancellationToken ct = default)
    {
        var animalCounts = await db.AdoptableAnimals
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        int Get(AdoptionStatus s) => animalCounts.FirstOrDefault(x => x.Status == s)?.Count ?? 0;

        return new AdoptionAdminStatsDto(
            TotalPublished: animalCounts.Sum(x => x.Count),
            TotalAvailable: Get(AdoptionStatus.Available),
            TotalInProcess: Get(AdoptionStatus.InProcess),
            TotalAdopted: Get(AdoptionStatus.Adopted),
            TotalPaused: Get(AdoptionStatus.Paused),
            TotalApplications: await db.AdoptionApplications.CountAsync(ct),
            TotalFairs: await db.AdoptionFairs.CountAsync(ct));
    }

    public async Task<(IReadOnlyList<AdoptablePet> Items, int Total)> GetAllAdminPagedAsync(
        AdoptionStatus? status, int skip, int take, CancellationToken ct = default)
    {
        var q = db.AdoptableAnimals.AsNoTracking();
        if (status.HasValue) q = q.Where(a => a.Status == status.Value);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(a => a.PublishedAt).Skip(skip).Take(take).ToListAsync(ct);
        return (items, total);
    }

    public async Task AddAnimalAsync(AdoptablePet animal, CancellationToken ct = default) =>
        await db.AdoptableAnimals.AddAsync(animal, ct);

    public void UpdateAnimal(AdoptablePet animal) => db.AdoptableAnimals.Update(animal);

    // ── Applications ──────────────────────────────────────────────────────────

    public Task<AdoptionApplication?> GetApplicationByIdAsync(Guid id, CancellationToken ct = default) =>
        db.AdoptionApplications.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<AdoptionApplication?> GetApplicationByApplicantAndAnimalAsync(
        Guid applicantUserId, Guid animalId, CancellationToken ct = default) =>
        db.AdoptionApplications.FirstOrDefaultAsync(
            a => a.ApplicantUserId == applicantUserId && a.AdoptablePetId == animalId, ct);

    public async Task<IReadOnlyList<AdoptionApplication>> GetApplicationsByAnimalAsync(
        Guid animalId, CancellationToken ct = default) =>
        await db.AdoptionApplications.AsNoTracking()
            .Where(a => a.AdoptablePetId == animalId)
            .OrderByDescending(a => a.AppliedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AdoptionApplication>> GetApplicationsByApplicantAsync(
        Guid applicantUserId, int skip, int take, CancellationToken ct = default) =>
        await db.AdoptionApplications.AsNoTracking()
            .Where(a => a.ApplicantUserId == applicantUserId)
            .OrderByDescending(a => a.AppliedAt)
            .Skip(skip).Take(take)
            .ToListAsync(ct);

    public Task<int> CountApplicationsByApplicantAsync(Guid applicantUserId, CancellationToken ct = default) =>
        db.AdoptionApplications.CountAsync(a => a.ApplicantUserId == applicantUserId, ct);

    public async Task AddApplicationAsync(AdoptionApplication application, CancellationToken ct = default) =>
        await db.AdoptionApplications.AddAsync(application, ct);

    public void UpdateApplication(AdoptionApplication application) =>
        db.AdoptionApplications.Update(application);

    // ── Fairs ─────────────────────────────────────────────────────────────────

    public Task<AdoptionFair?> GetFairByIdAsync(Guid id, CancellationToken ct = default) =>
        db.AdoptionFairs.FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<IReadOnlyList<AdoptionFair>> GetUpcomingFairsAsync(
        double? nearLat, double? nearLng, int radiusKm, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var q = db.AdoptionFairs.AsNoTracking()
            .Where(f => f.Status != FairStatus.Cancelled && f.EndsAt > now);

        if (nearLat.HasValue && nearLng.HasValue)
        {
            var latDelta = radiusKm / 111.0;
            var lngDelta = radiusKm / (111.0 * Math.Cos(nearLat.Value * Math.PI / 180.0));
            q = q.Where(f =>
                f.Lat >= nearLat.Value - latDelta && f.Lat <= nearLat.Value + latDelta &&
                f.Lng >= nearLng.Value - lngDelta && f.Lng <= nearLng.Value + lngDelta);
        }

        return await q.OrderBy(f => f.StartsAt).ToListAsync(ct);
    }

    public async Task AddFairAsync(AdoptionFair fair, CancellationToken ct = default) =>
        await db.AdoptionFairs.AddAsync(fair, ct);

    public void UpdateFair(AdoptionFair fair) => db.AdoptionFairs.Update(fair);
}

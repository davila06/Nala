using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Sightings.DTOs;
using PawTrack.Domain.LostPets;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.LostPets;

public sealed class LostPetRepository(PawTrackDbContext dbContext) : ILostPetRepository
{
    public async Task<LostPetEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.LostPetEvents
            .AsTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<LostPetEvent?> GetActiveByPetIdAsync(Guid petId, CancellationToken cancellationToken = default) =>
        await dbContext.LostPetEvents
            .AsTracking()
            .FirstOrDefaultAsync(
                e => e.PetId == petId && e.Status == LostPetStatus.Active,
                cancellationToken);

    public async Task<IReadOnlyList<LostPetEvent>> GetActiveLostPetsInBBoxAsync(
        double north,
        double south,
        double east,
        double west,
        CancellationToken cancellationToken = default)
    {
        // No-tracking read — map endpoint is public and read-only
        var results = await dbContext.LostPetEvents
            .Where(e =>
                e.Status == LostPetStatus.Active &&
                e.LastSeenLat != null && e.LastSeenLng != null &&
                e.LastSeenLat >= south && e.LastSeenLat <= north &&
                e.LastSeenLng >= west  && e.LastSeenLng <= east)
            .OrderByDescending(e => e.ReportedAt)
            .ToListAsync(cancellationToken);

        return results.AsReadOnly();
    }

    public async Task<IReadOnlyList<LostPetEvent>> GetActiveReportedBeforeAsync(
        DateTimeOffset reportedBefore,
        CancellationToken cancellationToken = default)
    {
        var results = await dbContext.LostPetEvents
            .Where(e =>
                e.Status == LostPetStatus.Active
                && e.ReportedAt <= reportedBefore)
            .OrderBy(e => e.ReportedAt)
            .ToListAsync(cancellationToken);

        return results.AsReadOnly();
    }

    public async Task AddAsync(LostPetEvent lostPetEvent, CancellationToken cancellationToken = default) =>
        await dbContext.LostPetEvents.AddAsync(lostPetEvent, cancellationToken);

    public void Update(LostPetEvent lostPetEvent) =>
        dbContext.LostPetEvents.Update(lostPetEvent);

    public async Task<IReadOnlyList<ActiveLostPetForMatchDto>> GetActiveLostPetsForMatchAsync(
        double north,
        double south,
        double east,
        double west,
        CancellationToken cancellationToken = default)
    {
        var rows = await (
            from lost in dbContext.LostPetEvents
            join pet in dbContext.Pets on lost.PetId equals pet.Id
            where lost.Status == LostPetStatus.Active
                  && lost.LastSeenLat != null
                  && lost.LastSeenLng != null
                  && lost.LastSeenLat >= south
                  && lost.LastSeenLat <= north
                  && lost.LastSeenLng >= west
                  && lost.LastSeenLng <= east
            orderby lost.ReportedAt descending
            select new ActiveLostPetForMatchDto(
                lost.Id,
                pet.Id,
                pet.OwnerId,
                pet.Name,
                pet.Species,
                pet.PhotoUrl,
                lost.LastSeenLat,
                lost.LastSeenLng,
                lost.ReportedAt))
            .ToListAsync(cancellationToken);

        return rows.AsReadOnly();
    }
}

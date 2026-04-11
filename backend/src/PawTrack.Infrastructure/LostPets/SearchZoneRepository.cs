using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.LostPets;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.LostPets;

internal sealed class SearchZoneRepository(PawTrackDbContext dbContext) : ISearchZoneRepository
{
    public async Task<IReadOnlyList<SearchZone>> GetByLostPetEventIdAsync(
        Guid lostPetEventId,
        CancellationToken cancellationToken)
    {
        return await dbContext.SearchZones
            .AsNoTracking()
            .Where(z => z.LostPetEventId == lostPetEventId)
            .OrderBy(z => z.Label)
            .ToListAsync(cancellationToken);
    }

    public async Task<SearchZone?> GetByIdAsync(Guid zoneId, CancellationToken cancellationToken)
    {
        // Tracking required so Update() and TryClaim/Clear/Release mutations are persisted.
        return await dbContext.SearchZones
            .FirstOrDefaultAsync(z => z.Id == zoneId, cancellationToken);
    }

    public async Task<bool> AnyForLostPetEventAsync(Guid lostPetEventId, CancellationToken cancellationToken)
    {
        return await dbContext.SearchZones
            .AnyAsync(z => z.LostPetEventId == lostPetEventId, cancellationToken);
    }

    public async Task AddAsync(SearchZone zone, CancellationToken cancellationToken)
    {
        await dbContext.SearchZones.AddAsync(zone, cancellationToken);
    }

    public void Update(SearchZone zone)
    {
        dbContext.SearchZones.Update(zone);
    }
}

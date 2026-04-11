using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Fosters;
using PawTrack.Domain.Pets;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Fosters;

public sealed class FosterVolunteerRepository(PawTrackDbContext dbContext) : IFosterVolunteerRepository
{
    public async Task<FosterVolunteer?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.Set<FosterVolunteer>()
            .AsTracking()
            .FirstOrDefaultAsync(f => f.UserId == userId, cancellationToken);

    public async Task AddAsync(FosterVolunteer volunteer, CancellationToken cancellationToken = default) =>
        await dbContext.Set<FosterVolunteer>().AddAsync(volunteer, cancellationToken);

    public void Update(FosterVolunteer volunteer) =>
        dbContext.Set<FosterVolunteer>().Update(volunteer);

    public async Task<IReadOnlyList<FosterVolunteerSuggestion>> GetNearbyAvailableAsync(
        double lat,
        double lng,
        PetSpecies foundSpecies,
        int radiusMetres,
        CancellationToken cancellationToken = default)
    {
        var (deltaLat, deltaLng) = GeoHelper.BoundingBoxDelta(lat, radiusMetres);
        var minLat = lat - deltaLat;
        var maxLat = lat + deltaLat;
        var minLng = lng - deltaLng;
        var maxLng = lng + deltaLng;

        var rows = await dbContext.Set<FosterVolunteer>()
            .AsNoTracking()
            .Where(f => f.IsAvailable
                && (f.AvailableUntil == null || f.AvailableUntil > DateTimeOffset.UtcNow)
                && f.HomeLat >= minLat && f.HomeLat <= maxLat
                && f.HomeLng >= minLng && f.HomeLng <= maxLng)
            .ToListAsync(cancellationToken);

        var suggestions = rows
            .Select(f =>
            {
                var distance = GeoHelper.DistanceMetres(lat, lng, f.HomeLat, f.HomeLng);
                var species = f.AcceptedSpecies;
                var speciesMatch = species.Contains(foundSpecies);

                return new FosterVolunteerSuggestion(
                    f.UserId,
                    f.FullName,
                    distance,
                    speciesMatch,
                    f.SizePreference,
                    f.MaxDays);
            })
            .Where(s => s.DistanceMetres <= radiusMetres)
            .OrderBy(s => s.DistanceMetres)
            .ToList();

        return suggestions.AsReadOnly();
    }
}

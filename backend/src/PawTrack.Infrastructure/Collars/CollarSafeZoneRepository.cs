using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Domain.Collars;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Collars;

public sealed class CollarSafeZoneRepository(PawTrackDbContext dbContext) : ICollarSafeZoneRepository
{
    public async Task AddAsync(CollarSafeZone zone, CancellationToken cancellationToken = default) =>
        await dbContext.CollarSafeZones.AddAsync(zone, cancellationToken);

    public Task<CollarSafeZone?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.CollarSafeZones.FirstOrDefaultAsync(z => z.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CollarSafeZone>> GetByCollarIdAsync(Guid collarId, CancellationToken cancellationToken = default) =>
        await dbContext.CollarSafeZones
            .Where(z => z.CollarId == collarId)
            .OrderBy(z => z.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CollarSafeZone>> GetEnabledByCollarIdAsync(Guid collarId, CancellationToken cancellationToken = default) =>
        await dbContext.CollarSafeZones
            .Where(z => z.CollarId == collarId && z.Enabled)
            .ToListAsync(cancellationToken);

    public void Update(CollarSafeZone zone) => dbContext.CollarSafeZones.Update(zone);
    public void Remove(CollarSafeZone zone) => dbContext.CollarSafeZones.Remove(zone);
}

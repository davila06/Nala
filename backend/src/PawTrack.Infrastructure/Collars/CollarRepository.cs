using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Domain.Collars;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Collars;

public sealed class CollarRepository(PawTrackDbContext dbContext) : ICollarRepository
{
    public Task<Collar?> GetActiveForPetAsync(Guid petId, CancellationToken cancellationToken = default) =>
        dbContext.Collars.FirstOrDefaultAsync(c => c.PetId == petId && c.IsActive, cancellationToken);

    public Task<Collar?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Collars.FindAsync([id], cancellationToken).AsTask();

    public async Task AddAsync(Collar collar, CancellationToken cancellationToken = default) =>
        await dbContext.Collars.AddAsync(collar, cancellationToken);

    public async Task<IReadOnlyList<CollarLocation>> GetLocationHistoryAsync(
        Guid collarId,
        DateTimeOffset since,
        int maxPoints,
        CancellationToken cancellationToken = default) =>
        await dbContext.CollarLocations
            .Where(l => l.CollarId == collarId && l.RecordedAt >= since)
            .OrderBy(l => l.RecordedAt)
            .Take(maxPoints)
            .ToListAsync(cancellationToken);

    public async Task AddLocationAsync(CollarLocation location, CancellationToken cancellationToken = default) =>
        await dbContext.CollarLocations.AddAsync(location, cancellationToken);

    public void Update(Collar collar) =>
        dbContext.Collars.Update(collar);
}

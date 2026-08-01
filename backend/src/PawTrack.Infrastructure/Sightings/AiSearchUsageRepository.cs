using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Sightings;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Sightings;

public sealed class AiSearchUsageRepository(PawTrackDbContext db) : IAiSearchUsageRepository
{
    public Task<AiSearchUsage?> GetAsync(Guid userId, int yearMonth, CancellationToken ct = default) =>
        db.AiSearchUsages
            .AsTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId && u.YearMonth == yearMonth, ct);

    public async Task AddAsync(AiSearchUsage usage, CancellationToken ct = default) =>
        await db.AiSearchUsages.AddAsync(usage, ct);

    public void Update(AiSearchUsage usage) =>
        db.AiSearchUsages.Update(usage);
}

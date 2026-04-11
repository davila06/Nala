using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Sightings;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Sightings;

public sealed class FoundPetRepository(PawTrackDbContext dbContext) : IFoundPetRepository
{
    public async Task AddAsync(FoundPetReport report, CancellationToken cancellationToken = default) =>
        await dbContext.FoundPetReports.AddAsync(report, cancellationToken);

    public async Task<FoundPetReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.FoundPetReports
            .AsTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<FoundPetReport>> GetOpenReportsAsync(
        int maxResults = 100, CancellationToken cancellationToken = default)
    {
        var results = await dbContext.FoundPetReports
            .AsNoTracking()
            .Where(r => r.Status == FoundPetStatus.Open)
            .OrderByDescending(r => r.ReportedAt)
            .Take(maxResults)
            .ToListAsync(cancellationToken);

        return results.AsReadOnly();
    }
}

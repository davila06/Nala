using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.SearchCoordination;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.SearchCoordination;

public sealed class SearchLocationSharingSessionRepository(PawTrackDbContext db)
    : ISearchLocationSharingSessionRepository
{
    public async Task AddAsync(
        SearchLocationSharingSession session,
        CancellationToken cancellationToken = default) =>
        await db.SearchLocationSharingSessions.AddAsync(session, cancellationToken);

    public Task<SearchLocationSharingSession?> GetActiveAsync(
        Guid lostEventId,
        string connectionId,
        CancellationToken cancellationToken = default) =>
        db.SearchLocationSharingSessions
            .FirstOrDefaultAsync(
                x => x.LostEventId == lostEventId &&
                     x.ConnectionId == connectionId &&
                     x.StoppedAt == null &&
                     x.ExpiredAt == null,
                cancellationToken);

    public async Task<IReadOnlyList<SearchLocationSharingSession>> GetExpiredActiveAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken = default) =>
        await db.SearchLocationSharingSessions
            .Where(x => x.ExpiresAt <= now && x.StoppedAt == null && x.ExpiredAt == null)
            .OrderBy(x => x.ExpiresAt)
            .Take(500)
            .ToListAsync(cancellationToken);

    public void Update(SearchLocationSharingSession session) => db.SearchLocationSharingSessions.Update(session);
}

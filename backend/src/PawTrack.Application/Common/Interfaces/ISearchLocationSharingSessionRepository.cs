using PawTrack.Domain.SearchCoordination;

namespace PawTrack.Application.Common.Interfaces;

public interface ISearchLocationSharingSessionRepository
{
    Task AddAsync(SearchLocationSharingSession session, CancellationToken cancellationToken = default);
    Task<SearchLocationSharingSession?> GetActiveAsync(
        Guid lostEventId,
        string connectionId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SearchLocationSharingSession>> GetExpiredActiveAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
    void Update(SearchLocationSharingSession session);
}

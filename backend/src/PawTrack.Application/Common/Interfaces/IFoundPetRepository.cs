using PawTrack.Domain.Sightings;

namespace PawTrack.Application.Common.Interfaces;

public interface IFoundPetRepository
{
    Task AddAsync(FoundPetReport report, CancellationToken cancellationToken = default);
    Task<FoundPetReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasRecentDuplicateAsync(
        string contactPhone,
        double foundLat,
        double foundLng,
        DateTimeOffset since,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FoundPetReport>> GetOpenReportsAsync(int maxResults = 100, CancellationToken cancellationToken = default);
}

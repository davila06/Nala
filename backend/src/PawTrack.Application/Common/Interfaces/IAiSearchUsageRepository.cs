using PawTrack.Domain.Sightings;

namespace PawTrack.Application.Common.Interfaces;

public interface IAiSearchUsageRepository
{
    Task<AiSearchUsage?> GetAsync(Guid userId, int yearMonth, CancellationToken ct = default);
    Task AddAsync(AiSearchUsage usage, CancellationToken ct = default);
    void Update(AiSearchUsage usage);
}

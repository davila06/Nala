using PawTrack.Domain.Medical;

namespace PawTrack.Application.Common.Interfaces;

public interface IActivityLogRepository
{
    Task<IReadOnlyList<ActivityLog>> GetByPetAndDateRangeAsync(
        Guid petId, DateOnly from, DateOnly to, CancellationToken ct = default);

    Task<ActivityLog?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(ActivityLog log, CancellationToken ct = default);
    void Delete(ActivityLog log);

    /// <summary>True when a Tractive-sourced entry already exists for this pet on this date.</summary>
    Task<bool> ExistsTractiveForDateAsync(Guid petId, DateOnly date, CancellationToken ct = default);
}

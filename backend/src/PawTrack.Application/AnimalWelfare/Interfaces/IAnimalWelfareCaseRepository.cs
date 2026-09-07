using PawTrack.Domain.AnimalWelfare;

namespace PawTrack.Application.AnimalWelfare.Interfaces;

public interface IAnimalWelfareCaseRepository
{
    Task<AnimalWelfareCase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AnimalWelfareCase?> GetByPublicCodeAsync(string publicCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AnimalWelfareCase>> GetQueueAsync(
        WelfareCaseStatus? status,
        WelfareSeverity? severity,
        string? canton,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AnimalWelfareCase>> GetAssignedAsync(Guid organizationUserId, int skip, int take, CancellationToken cancellationToken = default);
    Task AddAsync(AnimalWelfareCase welfareCase, CancellationToken cancellationToken = default);
    void Update(AnimalWelfareCase welfareCase);
}

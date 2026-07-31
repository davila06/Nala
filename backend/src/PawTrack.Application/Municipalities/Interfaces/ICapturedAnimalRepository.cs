using PawTrack.Domain.Municipalities;

namespace PawTrack.Application.Municipalities.Interfaces;

public interface ICapturedAnimalRepository
{
    Task<CapturedAnimal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<CapturedAnimal> Items, int Total)> SearchAsync(
        string? canton,
        CapturedAnimalStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(CapturedAnimal animal, CancellationToken cancellationToken = default);
    void Update(CapturedAnimal animal);
}

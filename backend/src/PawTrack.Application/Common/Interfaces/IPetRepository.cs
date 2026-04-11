using PawTrack.Domain.Pets;

namespace PawTrack.Application.Common.Interfaces;

public interface IPetRepository
{
    Task<Pet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Pet>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<Pet?> GetByMicrochipIdAsync(string microchipId, CancellationToken cancellationToken = default);
    Task AddAsync(Pet pet, CancellationToken cancellationToken = default);
    void Update(Pet pet);
    void Delete(Pet pet);
}

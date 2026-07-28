using PawTrack.Domain.Pets;

namespace PawTrack.Application.Common.Interfaces;

public interface IPetRepository
{
    Task<Pet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Pet>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<Pet?> GetByMicrochipIdAsync(string microchipId, CancellationToken cancellationToken = default);
    /// <summary>Batch fetch pets by a set of IDs. Returns only found pets; missing IDs are silently omitted.</summary>
    Task<IReadOnlyList<Pet>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task AddAsync(Pet pet, CancellationToken cancellationToken = default);
    void Update(Pet pet);
    void Delete(Pet pet);
}

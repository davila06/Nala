using PawTrack.Domain.Collars;

namespace PawTrack.Application.Collars.Interfaces;

public interface ICollarRepository
{
    Task<Collar?> GetActiveForPetAsync(Guid petId, CancellationToken cancellationToken = default);
    Task<Collar?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Collar collar, CancellationToken cancellationToken = default);
    Task AddLocationAsync(CollarLocation location, CancellationToken cancellationToken = default);
    void Update(Collar collar);
}

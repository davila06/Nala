using PawTrack.Domain.Collars;

namespace PawTrack.Application.Collars.Interfaces;

public interface ICollarHandoverCodeRepository
{
    Task AddAsync(CollarHandoverCode code, CancellationToken cancellationToken = default);
    Task<CollarHandoverCode?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns the most recent still-redeemable code for a collar, if any (only one active at a time).</summary>
    Task<CollarHandoverCode?> GetActiveForCollarAsync(Guid collarId, CancellationToken cancellationToken = default);

    void Update(CollarHandoverCode code);
}

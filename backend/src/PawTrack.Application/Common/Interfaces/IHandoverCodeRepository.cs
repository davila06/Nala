using PawTrack.Domain.Safety;

namespace PawTrack.Application.Common.Interfaces;

public interface IHandoverCodeRepository
{
    Task<HandoverCode?> GetActiveByLostPetEventIdAsync(
        Guid lostPetEventId,
        CancellationToken cancellationToken = default);

    Task AddAsync(HandoverCode code, CancellationToken cancellationToken = default);

    void Update(HandoverCode code);
}

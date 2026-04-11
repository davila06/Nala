using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Safety;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Safety;

public sealed class HandoverCodeRepository(PawTrackDbContext dbContext) : IHandoverCodeRepository
{
    public Task<HandoverCode?> GetActiveByLostPetEventIdAsync(
        Guid lostPetEventId,
        CancellationToken cancellationToken = default) =>
        dbContext.HandoverCodes
                 .FirstOrDefaultAsync(
                     h => h.LostPetEventId == lostPetEventId
                          && !h.IsUsed
                          && h.ExpiresAt > DateTimeOffset.UtcNow,
                     cancellationToken);

    public Task AddAsync(HandoverCode code, CancellationToken cancellationToken = default) =>
        dbContext.HandoverCodes.AddAsync(code, cancellationToken).AsTask();

    public void Update(HandoverCode code) =>
        dbContext.HandoverCodes.Update(code);
}

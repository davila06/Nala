using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Domain.Collars;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Collars;

public sealed class CollarHandoverCodeRepository(PawTrackDbContext dbContext) : ICollarHandoverCodeRepository
{
    public async Task AddAsync(CollarHandoverCode code, CancellationToken cancellationToken = default) =>
        await dbContext.CollarHandoverCodes.AddAsync(code, cancellationToken);

    public Task<CollarHandoverCode?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.CollarHandoverCodes.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<CollarHandoverCode?> GetActiveForCollarAsync(Guid collarId, CancellationToken cancellationToken = default) =>
        dbContext.CollarHandoverCodes
            .Where(c => c.CollarId == collarId && c.RedeemedAt == null && c.CancelledAt == null)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public void Update(CollarHandoverCode code) =>
        dbContext.CollarHandoverCodes.Update(code);
}

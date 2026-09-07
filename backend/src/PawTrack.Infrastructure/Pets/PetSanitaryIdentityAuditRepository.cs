using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Pets;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Pets;

public sealed class PetSanitaryIdentityAuditRepository(PawTrackDbContext dbContext)
    : IPetSanitaryIdentityAuditRepository
{
    public async Task AddAsync(PetSanitaryIdentityAuditLog log, CancellationToken cancellationToken = default) =>
        await dbContext.PetSanitaryIdentityAuditLogs.AddAsync(log, cancellationToken);

    public async Task<IReadOnlyList<PetSanitaryIdentityAuditLog>> GetByPetIdAsync(
        Guid petId,
        int take,
        CancellationToken cancellationToken = default) =>
        await dbContext.PetSanitaryIdentityAuditLogs
            .AsNoTracking()
            .Where(log => log.PetId == petId)
            .OrderByDescending(log => log.CreatedAt)
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync(cancellationToken);
}

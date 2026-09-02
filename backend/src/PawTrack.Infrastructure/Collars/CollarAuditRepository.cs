using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Domain.Collars;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Collars;

public sealed class CollarAuditRepository(PawTrackDbContext dbContext) : ICollarAuditRepository
{
    public async Task AddAsync(CollarAuditEntry entry, CancellationToken cancellationToken = default) =>
        await dbContext.CollarAuditEntries.AddAsync(entry, cancellationToken);

    public async Task<IReadOnlyList<CollarAuditEntry>> GetByCollarIdAsync(
        Guid collarId, int skip, int take, CancellationToken cancellationToken = default) =>
        await dbContext.CollarAuditEntries
            .Where(e => e.CollarId == collarId)
            .OrderByDescending(e => e.CreatedAt)
            .Skip(skip).Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CollarAuditEntry>> GetBySerialAsync(
        string serial, int skip, int take, CancellationToken cancellationToken = default)
    {
        var normalized = serial.ToUpperInvariant();
        return await dbContext.CollarAuditEntries
            .Where(e => e.Serial == normalized)
            .OrderByDescending(e => e.CreatedAt)
            .Skip(skip).Take(take)
            .ToListAsync(cancellationToken);
    }
}

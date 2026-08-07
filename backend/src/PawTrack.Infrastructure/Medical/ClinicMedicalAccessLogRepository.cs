using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Medical;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Medical;

public sealed class ClinicMedicalAccessLogRepository(PawTrackDbContext db)
    : IClinicMedicalAccessLogRepository
{
    public async Task AddAsync(ClinicMedicalAccessLog log, CancellationToken ct = default) =>
        await db.ClinicMedicalAccessLogs.AddAsync(log, ct);

    public async Task<IReadOnlyList<ClinicMedicalAccessLog>> GetByPetIdAsync(
        Guid petId, int limit = 50, CancellationToken ct = default) =>
        await db.ClinicMedicalAccessLogs.AsNoTracking()
            .Where(l => l.PetId == petId)
            .OrderByDescending(l => l.AccessedAt)
            .Take(limit)
            .ToListAsync(ct);
}

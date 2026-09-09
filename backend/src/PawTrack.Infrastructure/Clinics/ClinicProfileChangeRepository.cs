using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Clinics;

public sealed class ClinicProfileChangeRepository(PawTrackDbContext db) : IClinicProfileChangeRepository
{
    public Task AddAsync(ClinicProfileChange change, CancellationToken ct = default) => db.ClinicProfileChanges.AddAsync(change, ct).AsTask();
    public Task<ClinicProfileChange?> GetByIdAsync(Guid id, CancellationToken ct = default) => db.ClinicProfileChanges.FirstOrDefaultAsync(x => x.Id == id, ct);
    public async Task<IReadOnlyList<ClinicProfileChange>> GetPendingAsync(int take = 100, CancellationToken ct = default) =>
        await db.ClinicProfileChanges.AsNoTracking()
            .Where(x => x.Status == ClinicProfileChangeStatus.Pending)
            .OrderBy(x => x.CreatedAt).Take(Math.Clamp(take, 1, 500)).ToListAsync(ct);
    public void Update(ClinicProfileChange change) => db.ClinicProfileChanges.Update(change);
}
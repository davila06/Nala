using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Medical;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Medical;

public sealed class ClinicMedicalAccessGrantRepository(PawTrackDbContext dbContext)
    : IClinicMedicalAccessGrantRepository
{
    public Task<ClinicMedicalAccessGrant?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        dbContext.ClinicMedicalAccessGrants.FindAsync([id], ct).AsTask();

    public Task<ClinicMedicalAccessGrant?> GetActiveGrantAsync(
        Guid clinicId, Guid petId, CancellationToken ct = default) =>
        dbContext.ClinicMedicalAccessGrants
            .FirstOrDefaultAsync(g => g.ClinicId == clinicId
                                   && g.PetId == petId
                                   && g.IsActive
                                   && g.AcceptedAt != null, ct);

    public async Task<IReadOnlyList<ClinicMedicalAccessGrant>> GetByPetIdAsync(
        Guid petId, CancellationToken ct = default) =>
        await dbContext.ClinicMedicalAccessGrants
            .AsNoTracking()
            .Where(g => g.PetId == petId && g.RevokedAt == null)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ClinicMedicalAccessGrant>> GetByClinicIdAsync(
        Guid clinicId, CancellationToken ct = default) =>
        await dbContext.ClinicMedicalAccessGrants
            .AsNoTracking()
            .Where(g => g.ClinicId == clinicId && g.IsActive && g.AcceptedAt != null)
            .OrderByDescending(g => g.AcceptedAt)
            .ToListAsync(ct);

    public Task<bool> HasActiveGrantAsync(
        Guid clinicId, Guid petId, CancellationToken ct = default) =>
        dbContext.ClinicMedicalAccessGrants
            .AsNoTracking()
            .AnyAsync(g => g.ClinicId == clinicId
                        && g.PetId == petId
                        && g.IsActive
                        && g.AcceptedAt != null, ct);

    public Task<ClinicMedicalAccessGrant?> FindPendingByCodeHashAsync(
        string codeHash, CancellationToken ct = default) =>
        dbContext.ClinicMedicalAccessGrants
            .FirstOrDefaultAsync(g => g.CodeHash == codeHash
                                   && g.AcceptedAt == null
                                   && !g.IsActive, ct);

    public async Task AddAsync(ClinicMedicalAccessGrant grant, CancellationToken ct = default) =>
        await dbContext.ClinicMedicalAccessGrants.AddAsync(grant, ct);

    public void Update(ClinicMedicalAccessGrant grant) =>
        dbContext.ClinicMedicalAccessGrants.Update(grant);
}

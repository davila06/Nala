using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Medical;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Medical;

public sealed class ClinicMedicalExportRepository(PawTrackDbContext db) : IClinicMedicalExportRepository
{
    public Task<int> CountForClinicSinceAsync(Guid clinicId, DateTimeOffset since, CancellationToken ct = default) =>
        db.ClinicMedicalExports.CountAsync(x => x.ClinicId == clinicId && x.CreatedAt >= since, ct);
    public Task<bool> ExistsForPetSinceAsync(Guid clinicId, Guid petId, DateTimeOffset since, CancellationToken ct = default) =>
        db.ClinicMedicalExports.AnyAsync(x => x.ClinicId == clinicId && x.PetId == petId && x.CreatedAt >= since, ct);
    public async Task AddAsync(ClinicMedicalExport export, CancellationToken ct = default) => await db.ClinicMedicalExports.AddAsync(export, ct);
    public Task<ClinicMedicalExport?> GetByIdAsync(Guid id, CancellationToken ct = default) => db.ClinicMedicalExports.FirstOrDefaultAsync(x => x.Id == id, ct);
    public Task<int> DeleteExpiredBeforeAsync(DateTimeOffset cutoff, CancellationToken ct = default) =>
        db.ClinicMedicalExports.Where(x => x.Status == ClinicMedicalExportStatus.Expired && x.ExpiresAt < cutoff).ExecuteDeleteAsync(ct);
    public Task<int> ExpireBeforeAsync(DateTimeOffset cutoff, CancellationToken ct = default) =>
        db.ClinicMedicalExports.Where(x => x.Status == ClinicMedicalExportStatus.Completed && x.ExpiresAt < cutoff)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, ClinicMedicalExportStatus.Expired), ct);
}
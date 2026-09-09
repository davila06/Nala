using PawTrack.Domain.Medical;

namespace PawTrack.Application.Common.Interfaces;

public interface IClinicMedicalExportRepository
{
    Task<int> CountForClinicSinceAsync(Guid clinicId, DateTimeOffset since, CancellationToken ct = default);
    Task<bool> ExistsForPetSinceAsync(Guid clinicId, Guid petId, DateTimeOffset since, CancellationToken ct = default);
    Task AddAsync(ClinicMedicalExport export, CancellationToken ct = default);
    Task<ClinicMedicalExport?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<int> DeleteExpiredBeforeAsync(DateTimeOffset cutoff, CancellationToken ct = default);
    Task<int> ExpireBeforeAsync(DateTimeOffset cutoff, CancellationToken ct = default);
}
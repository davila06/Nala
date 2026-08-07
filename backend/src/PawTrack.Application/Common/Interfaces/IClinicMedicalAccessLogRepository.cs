using PawTrack.Domain.Medical;

namespace PawTrack.Application.Common.Interfaces;

public interface IClinicMedicalAccessLogRepository
{
    Task AddAsync(ClinicMedicalAccessLog log, CancellationToken ct = default);
    Task<IReadOnlyList<ClinicMedicalAccessLog>> GetByPetIdAsync(Guid petId, int limit = 50, CancellationToken ct = default);
}

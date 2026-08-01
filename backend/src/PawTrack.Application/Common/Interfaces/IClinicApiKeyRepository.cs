using PawTrack.Domain.Clinics;

namespace PawTrack.Application.Common.Interfaces;

public interface IClinicApiKeyRepository
{
    Task<ClinicApiKey?> GetByHashAsync(string keyHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicApiKey>> GetForClinicAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task AddAsync(ClinicApiKey key, CancellationToken cancellationToken = default);
    void Update(ClinicApiKey key);
}

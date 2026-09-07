using PawTrack.Domain.Certificates;

namespace PawTrack.Application.Certificates.Interfaces;

public interface IClinicVerificationRepository
{
    Task<ClinicVerification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ClinicVerification?> GetActiveForClinicAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task<ClinicVerification?> GetLatestForClinicAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicVerification>> GetPendingPagedAsync(int skip, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicVerification>> GetExpiringWithinAsync(int days, CancellationToken cancellationToken = default);
    Task<bool> HasActiveVerificationAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task AddAsync(ClinicVerification verification, CancellationToken cancellationToken = default);
    void Update(ClinicVerification verification);
}

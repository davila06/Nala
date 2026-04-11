using PawTrack.Domain.Clinics;

namespace PawTrack.Application.Common.Interfaces;

public interface IClinicRepository
{
    Task<Clinic?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Clinic?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Clinic?> GetByLicenseNumberAsync(string licenseNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Clinic>> GetAllPendingAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Clinic clinic, CancellationToken cancellationToken = default);
    void Update(Clinic clinic);
}

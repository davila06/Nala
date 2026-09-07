using PawTrack.Domain.Certificates;

namespace PawTrack.Application.Certificates.Interfaces;

public interface IClinicVeterinarianRepository
{
    Task<ClinicVeterinarian?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicVeterinarian>> GetActiveForClinicAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicVeterinarian>> GetByClinicAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicVeterinarian>> GetPendingPagedAsync(int skip, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicVeterinarian>> GetExpiringWithinAsync(int days, CancellationToken cancellationToken = default);
    Task<bool> LicenseExistsForClinicAsync(Guid clinicId, string licenseNumber, CancellationToken cancellationToken = default);
    Task AddAsync(ClinicVeterinarian veterinarian, CancellationToken cancellationToken = default);
    void Update(ClinicVeterinarian veterinarian);
}

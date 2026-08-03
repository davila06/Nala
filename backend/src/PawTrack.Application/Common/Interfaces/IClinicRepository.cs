using PawTrack.Domain.Clinics;

namespace PawTrack.Application.Common.Interfaces;

public interface IClinicRepository
{
    Task<Clinic?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Batch fetch by ID set. Missing IDs are silently omitted.</summary>
    Task<IReadOnlyList<Clinic>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<Clinic?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Clinic?> GetByLicenseNumberAsync(string licenseNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Clinic>> GetAllPendingAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Clinic>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    /// <summary>Returns active clinics at ClinicPlus or ClinicPartner tier within <paramref name="radiusKm"/> km.</summary>
    Task<IReadOnlyList<Clinic>> GetFeaturedNearAsync(double lat, double lng, double radiusKm, CancellationToken cancellationToken = default);
    Task AddAsync(Clinic clinic, CancellationToken cancellationToken = default);
    void Update(Clinic clinic);
}

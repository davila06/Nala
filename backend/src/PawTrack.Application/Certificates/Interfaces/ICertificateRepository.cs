using PawTrack.Domain.Certificates;

namespace PawTrack.Application.Certificates.Interfaces;

public interface ICertificateRepository
{
    Task<VetCertificate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<VetCertificate?> GetByVerificationCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VetCertificate>> GetForPetAsync(Guid petId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VetCertificate>> GetForClinicAsync(Guid clinicId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task AddAsync(VetCertificate certificate, CancellationToken cancellationToken = default);
    void Update(VetCertificate certificate);
}

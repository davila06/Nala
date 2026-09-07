using PawTrack.Domain.Certificates;

namespace PawTrack.Application.Certificates.Interfaces;

public interface IVaccinePassportRepository
{
    Task<VaccinePassport?> GetByCertificateIdAsync(Guid certificateId, CancellationToken cancellationToken = default);
    Task AddAsync(VaccinePassport passport, CancellationToken cancellationToken = default);
}

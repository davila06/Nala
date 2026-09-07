using PawTrack.Domain.Certificates;

namespace PawTrack.Application.Certificates.Interfaces;

public interface ICertificateAuditLogRepository
{
    Task AddAsync(CertificateAuditLog log, CancellationToken cancellationToken = default);
}

using PawTrack.Domain.Certificates;

namespace PawTrack.Application.Certificates.Interfaces;

public interface IVerificationAuditLogRepository
{
    Task AddAsync(VerificationAuditLog log, CancellationToken cancellationToken = default);
}

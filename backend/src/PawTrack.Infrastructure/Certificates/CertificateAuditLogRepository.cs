using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Certificates;

public sealed class CertificateAuditLogRepository(PawTrackDbContext dbContext) : ICertificateAuditLogRepository
{
    public async Task AddAsync(CertificateAuditLog log, CancellationToken cancellationToken = default) =>
        await dbContext.CertificateAuditLogs.AddAsync(log, cancellationToken);
}

using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Certificates;

public sealed class VerificationAuditLogRepository(PawTrackDbContext dbContext) : IVerificationAuditLogRepository
{
    public async Task AddAsync(VerificationAuditLog log, CancellationToken cancellationToken = default) =>
        await dbContext.VerificationAuditLogs.AddAsync(log, cancellationToken);
}

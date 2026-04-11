using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Safety;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Safety;

public sealed class FraudReportRepository(PawTrackDbContext dbContext) : IFraudReportRepository
{
    public Task AddAsync(FraudReport report, CancellationToken cancellationToken = default) =>
        dbContext.FraudReports.AddAsync(report, cancellationToken).AsTask();

    public Task<int> CountRecentByTargetUserAsync(
        Guid targetUserId,
        TimeSpan window,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow - window;
        return dbContext.FraudReports
            .AsNoTracking()
            .CountAsync(
                f => f.TargetUserId == targetUserId && f.ReportedAt >= cutoff,
                cancellationToken);
    }

    public Task<int> CountRecentByIpHashAsync(
        string ipHash,
        TimeSpan window,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow - window;
        return dbContext.FraudReports
            .AsNoTracking()
            .CountAsync(
                f => f.ReporterIpHash == ipHash && f.ReportedAt >= cutoff,
                cancellationToken);
    }
}

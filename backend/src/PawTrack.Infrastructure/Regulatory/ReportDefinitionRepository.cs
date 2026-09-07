using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Regulatory.Interfaces;
using PawTrack.Domain.Regulatory;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Regulatory;

public sealed class ReportDefinitionRepository(PawTrackDbContext dbContext) : IReportDefinitionRepository
{
    public Task<ReportDefinition?> GetActiveAsync(
        ReportType reportType,
        ExportScope scope,
        CancellationToken cancellationToken = default) =>
        dbContext.ReportDefinitions.AsNoTracking()
            .Where(d => d.ReportType == reportType && d.Scope == scope && d.IsActive)
            .OrderByDescending(d => d.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ReportDefinition>> GetActiveForScopeAsync(
        ExportScope scope,
        CancellationToken cancellationToken = default) =>
        await dbContext.ReportDefinitions.AsNoTracking()
            .Where(d => d.Scope == scope && d.IsActive)
            .OrderBy(d => d.Code)
            .Take(100)
            .ToListAsync(cancellationToken);
}

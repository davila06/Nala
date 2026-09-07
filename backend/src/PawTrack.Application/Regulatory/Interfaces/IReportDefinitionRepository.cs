using PawTrack.Domain.Regulatory;

namespace PawTrack.Application.Regulatory.Interfaces;

public interface IReportDefinitionRepository
{
    Task<ReportDefinition?> GetActiveAsync(ReportType reportType, ExportScope scope, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReportDefinition>> GetActiveForScopeAsync(ExportScope scope, CancellationToken cancellationToken = default);
}

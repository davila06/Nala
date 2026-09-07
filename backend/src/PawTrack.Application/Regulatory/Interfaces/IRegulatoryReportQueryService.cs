using PawTrack.Application.Regulatory.Dtos;
using PawTrack.Domain.Regulatory;

namespace PawTrack.Application.Regulatory.Interfaces;

public interface IRegulatoryReportQueryService
{
    Task<NalaOverviewDto> GetOverviewAsync(DateOnly periodStart, DateOnly periodEnd, CancellationToken cancellationToken = default);
    Task<RegulatoryReportData> GetReportDataAsync(
        ReportType reportType,
        DateOnly periodStart,
        DateOnly periodEnd,
        RegulatoryReportFilter filter,
        CancellationToken cancellationToken = default);
    Task<PublicImpactStatsDto> GetPublicImpactStatsAsync(DateOnly periodStart, DateOnly periodEnd, int suppressionThreshold, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NalaMapCellDto>> GetMapCellsAsync(
        double south,
        double north,
        double west,
        double east,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NalaTrendPointDto>> GetTrendsAsync(
        DateOnly periodStart,
        DateOnly periodEnd,
        string? canton,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NalaCantonSummaryDto>> GetCantonSummaryAsync(
        DateOnly periodStart,
        DateOnly periodEnd,
        int suppressionThreshold,
        CancellationToken cancellationToken = default);
    Task<NalaInstitutionPerformanceDto> GetInstitutionPerformanceAsync(
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken cancellationToken = default);
}

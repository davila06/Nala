using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.ProductAnalytics;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.ProductAnalytics;

public sealed class ProductEventRepository(PawTrackDbContext db) : IProductEventRepository
{
    public Task<bool> ExistsByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        db.ProductEvents.AsNoTracking().AnyAsync(x => x.EventId == eventId, cancellationToken);

    public async Task AddAsync(ProductEvent productEvent, CancellationToken cancellationToken = default) =>
        await db.ProductEvents.AddAsync(productEvent, cancellationToken);

    public Task<int> DeleteOccurredBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default) =>
        db.ProductEvents.Where(x => x.OccurredAt < cutoff).ExecuteDeleteAsync(cancellationToken);

    public async Task<IReadOnlyList<ProductEventCount>> CountByEventNameAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string? canton,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.ProductEvents.AsNoTracking()
            .Where(x => x.OccurredAt >= from && x.OccurredAt < to);

        if (!string.IsNullOrWhiteSpace(canton))
            query = query.Where(x => x.Canton == canton);

        if (!string.IsNullOrWhiteSpace(correlationId))
            query = query.Where(x => x.CorrelationId == correlationId);

        var rows = await query
            .GroupBy(x => x.EventName)
            .Select(group => new
            {
                EventName = group.Key,
                Count = group.Count(),
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new ProductEventCount(row.EventName, row.Count))
            .ToList();
    }

    public async Task<IReadOnlyList<ProductCohortMetric>> GetPerformanceByCohortAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string? canton,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            WITH Journeys AS (
                SELECT
                    COALESCE(CONVERT(nvarchar(36), [PetId]), [AnonymousId]) AS [JourneyId],
                    COALESCE(MAX(NULLIF([Canton], '')), N'Sin especificar') AS [Canton],
                    MIN(CASE WHEN [EventName] = N'PetRegistered' THEN [OccurredAt] END) AS [RegisteredAt],
                    MIN(CASE WHEN [EventName] IN (N'PetProfileCompleted', N'QrActivated') THEN [OccurredAt] END) AS [ActivatedAt],
                    MIN(CASE WHEN [EventName] = N'LostPetReported' THEN [OccurredAt] END) AS [LostAt],
                    MIN(CASE WHEN [EventName] IN (N'SightingCreated', N'FirstResponseRecorded') THEN [OccurredAt] END) AS [FirstResponseAt],
                    MIN(CASE WHEN [EventName] IN (N'HandoverCompleted', N'PetReunited') THEN [OccurredAt] END) AS [ReunitedAt]
                FROM [ProductEvents]
                WHERE [OccurredAt] >= {0} AND [OccurredAt] < {1}
                  AND ({2} IS NULL OR [Canton] = {2})
                GROUP BY COALESCE(CONVERT(nvarchar(36), [PetId]), [AnonymousId])
            )
            SELECT
                CONCAT(DATEPART(year, COALESCE([RegisteredAt], [LostAt])), '-',
                    RIGHT(CONCAT('0', DATEPART(month, COALESCE([RegisteredAt], [LostAt]))), 2)) AS [Cohort],
                [Canton],
                COUNT(CASE WHEN [RegisteredAt] IS NOT NULL THEN 1 END) AS [RegisteredPets],
                COUNT(CASE WHEN [RegisteredAt] IS NOT NULL AND [ActivatedAt] >= [RegisteredAt] THEN 1 END) AS [ActivatedPets],
                COUNT(CASE WHEN [LostAt] IS NOT NULL THEN 1 END) AS [LostReports],
                COUNT(CASE WHEN [LostAt] IS NOT NULL AND [ReunitedAt] >= [LostAt] THEN 1 END) AS [ReunitedReports],
                AVG(CASE WHEN [FirstResponseAt] >= [LostAt]
                    THEN CAST(DATEDIFF_BIG(second, [LostAt], [FirstResponseAt]) AS float) / 60 END) AS [AverageFirstResponseMinutes],
                AVG(CASE WHEN [ReunitedAt] >= [LostAt]
                    THEN CAST(DATEDIFF_BIG(second, [LostAt], [ReunitedAt]) AS float) / 60 END) AS [AverageReunionMinutes],
                CASE WHEN COUNT(CASE WHEN [LostAt] IS NOT NULL THEN 1 END) = 0 THEN 0
                    ELSE 100.0 * COUNT(CASE WHEN [ReunitedAt] >= [LostAt] THEN 1 END)
                        / COUNT(CASE WHEN [LostAt] IS NOT NULL THEN 1 END) END AS [RecoveryRatePercent],
                CASE WHEN COUNT(CASE WHEN [FirstResponseAt] >= [LostAt] THEN 1 END) = 0 THEN 0
                    ELSE 100.0 * COUNT(CASE WHEN DATEDIFF(minute, [LostAt], [FirstResponseAt]) <= 360 THEN 1 END)
                        / COUNT(CASE WHEN [FirstResponseAt] >= [LostAt] THEN 1 END) END AS [FirstResponseSloPercent]
            FROM Journeys
            WHERE [RegisteredAt] IS NOT NULL OR [LostAt] IS NOT NULL
            GROUP BY
                CONCAT(DATEPART(year, COALESCE([RegisteredAt], [LostAt])), '-',
                    RIGHT(CONCAT('0', DATEPART(month, COALESCE([RegisteredAt], [LostAt]))), 2)),
                [Canton]
            ORDER BY [Cohort] DESC, [Canton]
            """;

        var rows = await db.Database.SqlQueryRaw<ProductCohortMetricRow>(
                sql,
                from,
                to,
                string.IsNullOrWhiteSpace(canton) ? DBNull.Value : canton.Trim())
            .ToListAsync(cancellationToken);

        return rows.Select(row => new ProductCohortMetric(
            row.Cohort,
            row.Canton,
            row.RegisteredPets,
            row.ActivatedPets,
            row.LostReports,
            row.ReunitedReports,
            row.AverageFirstResponseMinutes,
            row.AverageReunionMinutes,
            Math.Round(row.RecoveryRatePercent, 2),
            Math.Round(row.FirstResponseSloPercent, 2)))
            .ToList();
    }

    private sealed class ProductCohortMetricRow
    {
        public string Cohort { get; init; } = string.Empty;
        public string Canton { get; init; } = string.Empty;
        public int RegisteredPets { get; init; }
        public int ActivatedPets { get; init; }
        public int LostReports { get; init; }
        public int ReunitedReports { get; init; }
        public double? AverageFirstResponseMinutes { get; init; }
        public double? AverageReunionMinutes { get; init; }
        public double RecoveryRatePercent { get; init; }
        public double FirstResponseSloPercent { get; init; }
    }
}

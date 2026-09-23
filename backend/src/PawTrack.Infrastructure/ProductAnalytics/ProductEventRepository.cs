using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Pets;
using PawTrack.Domain.ProductAnalytics;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.ProductAnalytics;

public sealed class ProductEventRepository(PawTrackDbContext db) : IProductEventRepository
{
    public Task<bool> ExistsByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        db.ProductEvents.AsNoTracking().AnyAsync(x => x.EventId == eventId, cancellationToken);

    public Task<bool> ExistsByEventNameAndCorrelationIdAsync(
        string eventName,
        string correlationId,
        CancellationToken cancellationToken = default) =>
        db.ProductEvents.AsNoTracking().AnyAsync(
            x => x.EventName == eventName && x.CorrelationId == correlationId,
            cancellationToken);

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
        string? channel = null,
        string? species = null,
        Guid? tenantId = null,
        string? tenantType = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            WITH Journeys AS (
                SELECT
                    COALESCE(NULLIF([CorrelationId], ''), COALESCE(CONVERT(nvarchar(36), [PetId]), [AnonymousId])) AS [JourneyId],
                    COALESCE(MAX(NULLIF([Canton], '')), N'Sin especificar') AS [Canton],
                    COALESCE(MAX(NULLIF([Source], '')), N'Sin especificar') AS [Channel],
                    COALESCE(MAX(CASE p.[Species]
                        WHEN 0 THEN N'Dog' WHEN 1 THEN N'Cat' WHEN 2 THEN N'Bird'
                        WHEN 3 THEN N'Rabbit' WHEN 4 THEN N'Other' END), N'Sin especificar') AS [Species],
                    MIN(CASE WHEN [EventName] = N'PetRegistered' THEN [OccurredAt] END) AS [RegisteredAt],
                    MIN(CASE WHEN [EventName] IN (N'PetProfileCompleted', N'QrActivated') THEN [OccurredAt] END) AS [ActivatedAt],
                    MIN(CASE WHEN [EventName] = N'LostPetReported' THEN [OccurredAt] END) AS [LostAt],
                    MIN(CASE WHEN [EventName] IN (N'SightingCreated', N'FirstResponseRecorded') THEN [OccurredAt] END) AS [FirstResponseAt],
                    MIN(CASE WHEN [EventName] IN (N'HandoverCompleted', N'PetReunited') THEN [OccurredAt] END) AS [ReunitedAt]
                                FROM [ProductEvents] AS e
                                LEFT JOIN [Pets] AS p ON e.[PetId] = p.[Id]
                                WHERE e.[OccurredAt] >= {0} AND e.[OccurredAt] < {1}
                                    AND ({2} IS NULL OR e.[Canton] = {2})
                                    AND ({3} IS NULL OR e.[Source] = {3})
                                    AND ({4} IS NULL OR p.[Species] = {4})
                                      AND ({5} IS NULL OR e.[TenantId] = {5})
                                      AND ({6} IS NULL OR e.[TenantType] = {6})
                                GROUP BY COALESCE(NULLIF(e.[CorrelationId], ''), COALESCE(CONVERT(nvarchar(36), e.[PetId]), e.[AnonymousId]))
            ), JourneyMetrics AS (
                SELECT
                    [JourneyId],
                    CONCAT(DATEPART(year, COALESCE([RegisteredAt], [LostAt])), '-',
                        RIGHT(CONCAT('0', DATEPART(month, COALESCE([RegisteredAt], [LostAt]))), 2)) AS [Cohort],
                    [Canton],
                    [Channel],
                    [Species],
                    [RegisteredAt],
                    [ActivatedAt],
                    [LostAt],
                    [ReunitedAt],
                    CASE WHEN [FirstResponseAt] >= [LostAt]
                        THEN CAST(DATEDIFF_BIG(second, [LostAt], [FirstResponseAt]) AS float) / 60 END AS [FirstResponseMinutes],
                    CASE WHEN [ReunitedAt] >= [LostAt]
                        THEN CAST(DATEDIFF_BIG(second, [LostAt], [ReunitedAt]) AS float) / 60 END AS [ReunionMinutes]
                FROM Journeys
                WHERE [RegisteredAt] IS NOT NULL OR [LostAt] IS NOT NULL
            ), WithMedians AS (
                SELECT *,
                    PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY [FirstResponseMinutes])
                        OVER (PARTITION BY [Cohort], [Canton]) AS [MedianFirstResponseMinutes],
                    PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY [ReunionMinutes])
                        OVER (PARTITION BY [Cohort], [Canton]) AS [MedianReunionMinutes],
                    PERCENTILE_CONT(0.9) WITHIN GROUP (ORDER BY [FirstResponseMinutes])
                        OVER (PARTITION BY [Cohort], [Canton]) AS [P90FirstResponseMinutes],
                    PERCENTILE_CONT(0.9) WITHIN GROUP (ORDER BY [ReunionMinutes])
                        OVER (PARTITION BY [Cohort], [Canton]) AS [P90ReunionMinutes]
                FROM JourneyMetrics
            )
            SELECT
                [Cohort],
                [Canton],
                [Channel],
                [Species],
                COUNT(CASE WHEN [RegisteredAt] IS NOT NULL THEN 1 END) AS [RegisteredPets],
                COUNT(CASE WHEN [RegisteredAt] IS NOT NULL AND [ActivatedAt] >= [RegisteredAt] THEN 1 END) AS [ActivatedPets],
                COUNT(CASE WHEN [LostAt] IS NOT NULL THEN 1 END) AS [LostReports],
                COUNT(CASE WHEN [LostAt] IS NOT NULL AND [ReunitedAt] >= [LostAt] THEN 1 END) AS [ReunitedReports],
                MAX([MedianFirstResponseMinutes]) AS [MedianFirstResponseMinutes],
                MAX([MedianReunionMinutes]) AS [MedianReunionMinutes],
                MAX([P90FirstResponseMinutes]) AS [P90FirstResponseMinutes],
                MAX([P90ReunionMinutes]) AS [P90ReunionMinutes],
                CAST(CASE WHEN COUNT(CASE WHEN [LostAt] IS NOT NULL THEN 1 END) = 0 THEN 0.0
                    ELSE 100.0 * COUNT(CASE WHEN [ReunitedAt] >= [LostAt] THEN 1 END)
                        / COUNT(CASE WHEN [LostAt] IS NOT NULL THEN 1 END) END AS float) AS [RecoveryRatePercent],
                CAST(CASE WHEN COUNT(CASE WHEN [FirstResponseMinutes] IS NOT NULL THEN 1 END) = 0 THEN 0.0
                    ELSE 100.0 * COUNT(CASE WHEN [FirstResponseMinutes] <= 360 THEN 1 END)
                        / COUNT(CASE WHEN [FirstResponseMinutes] IS NOT NULL THEN 1 END) END AS float) AS [FirstResponseSloPercent]
            FROM WithMedians
                GROUP BY [Cohort], [Canton], [Channel], [Species]
            ORDER BY [Cohort] DESC, [Canton], [Channel], [Species]
            """;

        var rows = await db.Database.SqlQueryRaw<ProductCohortMetricRow>(
                sql,
                from,
                to,
                string.IsNullOrWhiteSpace(canton) ? DBNull.Value : canton.Trim(),
                string.IsNullOrWhiteSpace(channel) ? DBNull.Value : channel.Trim(),
                ParseSpecies(species),
                tenantId.HasValue ? tenantId.Value : DBNull.Value,
                string.IsNullOrWhiteSpace(tenantType) ? DBNull.Value : tenantType.Trim())

            .ToListAsync(cancellationToken);

        return rows.Select(row => new ProductCohortMetric(
            row.Cohort,
            row.Canton,
            row.Channel,
            row.Species,
            row.RegisteredPets,
            row.ActivatedPets,
            row.LostReports,
            row.ReunitedReports,
            row.MedianFirstResponseMinutes,
            row.MedianReunionMinutes,
            row.P90FirstResponseMinutes,
            row.P90ReunionMinutes,
            Math.Round(row.RecoveryRatePercent, 2),
            Math.Round(row.FirstResponseSloPercent, 2)))
            .ToList();
    }

    public async Task<ActiveProtectedCounts> GetActiveProtectedCountsAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default)
    {
        var activityNames = new[]
        {
            "QrScanned", "SightingCreated", "FirstResponseRecorded", "PetReunited",
            "HandoverCompleted", "HealthRecordUpdated", "AlertCreated", "AdoptionCompleted",
        };

        var activity = db.Pets
            .AsNoTracking()
            .Where(pet => pet.Status == PetStatus.Active &&
                (pet.PhotoUrl != null || pet.MicrochipId != null))
            .Join(
                db.ProductEvents.AsNoTracking(),
                pet => pet.Id,
                productEvent => productEvent.PetId,
                (pet, productEvent) => new { pet.Id, productEvent.EventName, productEvent.OccurredAt })
            .Where(row => activityNames.Contains(row.EventName) && row.OccurredAt <= asOf);

        async Task<int> CountSinceAsync(int days) => await activity
            .Where(row => row.OccurredAt >= asOf.AddDays(-days))
            .Select(row => row.Id)
            .Distinct()
            .CountAsync(cancellationToken);

        return new ActiveProtectedCounts(
            await CountSinceAsync(30),
            await CountSinceAsync(90),
            await CountSinceAsync(180));
    }

    private static object ParseSpecies(string? species)
    {
        if (string.IsNullOrWhiteSpace(species))
            return DBNull.Value;

        return Enum.TryParse<PetSpecies>(species.Trim(), true, out var parsed)
            ? (int)parsed
            : DBNull.Value;
    }

    private sealed class ProductCohortMetricRow
    {
        public string Cohort { get; init; } = string.Empty;
        public string Canton { get; init; } = string.Empty;
        public string Channel { get; init; } = string.Empty;
        public string Species { get; init; } = string.Empty;
        public int RegisteredPets { get; init; }
        public int ActivatedPets { get; init; }
        public int LostReports { get; init; }
        public int ReunitedReports { get; init; }
        public double? MedianFirstResponseMinutes { get; init; }
        public double? MedianReunionMinutes { get; init; }
        public double? P90FirstResponseMinutes { get; init; }
        public double? P90ReunionMinutes { get; init; }
        public double RecoveryRatePercent { get; init; }
        public double FirstResponseSloPercent { get; init; }
    }
}

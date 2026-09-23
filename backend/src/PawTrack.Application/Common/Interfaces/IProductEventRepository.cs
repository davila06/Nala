using PawTrack.Domain.ProductAnalytics;

namespace PawTrack.Application.Common.Interfaces;

public interface IProductEventRepository
{
    Task<bool> ExistsByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEventNameAndCorrelationIdAsync(
        string eventName,
        string correlationId,
        CancellationToken cancellationToken = default);
    Task AddAsync(ProductEvent productEvent, CancellationToken cancellationToken = default);
    Task<int> DeleteOccurredBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductEventCount>> CountByEventNameAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string? canton,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductCohortMetric>> GetPerformanceByCohortAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string? canton,
        CancellationToken cancellationToken = default);
}

public sealed record ProductEventCount(string EventName, int Count);

public sealed record ProductCohortMetric(
    string Cohort,
    string Canton,
    int RegisteredPets,
    int ActivatedPets,
    int LostReports,
    int ReunitedReports,
    double? MedianFirstResponseMinutes,
    double? MedianReunionMinutes,
    double? P90FirstResponseMinutes,
    double? P90ReunionMinutes,
    double RecoveryRatePercent,
    double FirstResponseSloPercent)
{
    public double ActivationRatePercent => RegisteredPets == 0
        ? 0
        : Math.Round(ActivatedPets * 100d / RegisteredPets, 2);
}

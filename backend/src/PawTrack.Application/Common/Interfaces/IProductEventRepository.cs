using PawTrack.Domain.ProductAnalytics;

namespace PawTrack.Application.Common.Interfaces;

public interface IProductEventRepository
{
    Task<bool> ExistsByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task AddAsync(ProductEvent productEvent, CancellationToken cancellationToken = default);
    Task<int> DeleteOccurredBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductEventCount>> CountByEventNameAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string? canton,
        CancellationToken cancellationToken = default);
}

public sealed record ProductEventCount(string EventName, int Count);
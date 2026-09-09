using PawTrack.Domain.Advertising;

namespace PawTrack.Application.Common.Interfaces;

public interface IBillboardRepository
{
    Task<Billboard?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Billboard>> GetActiveByPlacementAsync(BillboardPlacement placement, CancellationToken ct = default);
    Task<IReadOnlyList<Billboard>> GetAllAsync(int skip, int take, CancellationToken ct = default);
    Task<int> CountAllAsync(CancellationToken ct = default);
    Task<bool> HasDeliveryEventAsync(Guid billboardId, BillboardDeliveryEventType eventType, string eventKeyHash, CancellationToken ct = default);
    Task<int> CountImpressionsByVisitorTodayAsync(Guid billboardId, string visitorHash, CancellationToken ct = default);
    Task AddDeliveryEventAsync(BillboardDeliveryEvent deliveryEvent, CancellationToken ct = default);
    Task<BillboardCampaignMetrics> GetMetricsAsync(Guid billboardId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task AddAsync(Billboard billboard, CancellationToken ct = default);
    Task DeleteAsync(Billboard billboard, CancellationToken ct = default);
    void Update(Billboard billboard);
}

public sealed record BillboardCampaignMetrics(
    Guid BillboardId,
    int Impressions,
    int Clicks,
    int Conversions,
    decimal ClickThroughRate,
    IReadOnlyDictionary<string, int> ImpressionsByCanton,
    IReadOnlyDictionary<string, int> ImpressionsByPlacement);

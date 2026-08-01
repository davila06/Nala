using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Subscriptions.Services;

public interface ISubscriptionService
{
    Task<SubscriptionTier> GetActiveUserTierAsync(Guid userId, CancellationToken ct = default);
    Task<bool> IsAtLeastPlusAsync(Guid userId, CancellationToken ct = default);
    Task<bool> IsFamiliaAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Returns 1 (free) / 3 (Plus) / -1 (unlimited, Familia).</summary>
    Task<int> GetPetLimitAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Returns 5 (free) / 50 (Plus+).</summary>
    Task<int> GetScanHistoryLimitAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Returns 3 (free) / null (unlimited for Plus+).</summary>
    Task<int?> GetMonthlyAiSearchLimitAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns a tier multiplier applied to the species-based alert radius.
    /// Free = 1.0 | Plus = 3.3 (≈ 10 km / 3 km base) | Familia = -1 (no cap).
    /// </summary>
    Task<double> GetAlertRadiusMultiplierAsync(Guid userId, CancellationToken ct = default);
}

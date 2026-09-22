using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Infrastructure.Subscriptions;

public sealed class SubscriptionService(
    ISubscriptionRepository repository,
    IEntitlementService? entitlementService = null) : ISubscriptionService
{
    public async Task<SubscriptionTier> GetActiveUserTierAsync(Guid userId, CancellationToken ct = default)
    {
        var sub = await repository.GetActiveForUserAsync(userId, ct);
        // Belt-and-suspenders: repository already filters by ExpiresAt, but IsActive is the
        // domain's authoritative definition of "still entitled" and must never be bypassed.
        return sub is not null && sub.IsActive ? sub.Tier : SubscriptionTier.Free;
    }

    public async Task<bool> IsAtLeastPlusAsync(Guid userId, CancellationToken ct = default)
    {
        var tier = await GetActiveUserTierAsync(userId, ct);
        return tier is SubscriptionTier.UserPlus or SubscriptionTier.UserFamilia;
    }

    public async Task<bool> IsFamiliaAsync(Guid userId, CancellationToken ct = default)
    {
        var tier = await GetActiveUserTierAsync(userId, ct);
        return tier == SubscriptionTier.UserFamilia;
    }

    public async Task<int> GetPetLimitAsync(Guid userId, CancellationToken ct = default)
    {
        var entitlement = await GetNumericEntitlementAsync(userId, "MaxPets", ct);
        if (entitlement.HasValue) return (int)entitlement.Value;

        return await GetActiveUserTierAsync(userId, ct) switch
        {
            SubscriptionTier.UserFamilia => -1,
            SubscriptionTier.UserPlus => 3,
            _ => 1,
        };
    }

    public async Task<int> GetScanHistoryLimitAsync(Guid userId, CancellationToken ct = default)
    {
        var entitlement = await GetNumericEntitlementAsync(userId, "ScanHistoryRetentionDays", ct);
        if (entitlement.HasValue) return (int)entitlement.Value;
        return await IsAtLeastPlusAsync(userId, ct) ? int.MaxValue : 5;
    }

    public async Task<int?> GetMonthlyAiSearchLimitAsync(Guid userId, CancellationToken ct = default)
    {
        var entitlement = await GetNumericEntitlementAsync(userId, "AiMatchesPerCycle", ct);
        if (entitlement.HasValue) return (int)entitlement.Value;
        return await IsAtLeastPlusAsync(userId, ct) ? null : 3;
    }

    public async Task<double> GetAlertRadiusMultiplierAsync(Guid userId, CancellationToken ct = default) =>
        await GetActiveUserTierAsync(userId, ct) switch
        {
            SubscriptionTier.UserFamilia => -1.0, // no cap
            SubscriptionTier.UserPlus => 3.33,
            _ => 1.0,
        };

    private async Task<decimal?> GetNumericEntitlementAsync(
        Guid userId,
        string key,
        CancellationToken ct)
    {
        if (entitlementService is null) return null;
        var snapshot = await entitlementService.GetSnapshotAsync(userId, ct);
        return snapshot.Entitlements.TryGetValue(key, out var entitlement)
            ? entitlement.NumericValue
            : null;
    }
}

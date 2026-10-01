using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Subscriptions.DTOs;

public sealed record SubscriptionPlanDto(
    Guid Id,
    SubscriptionTier Tier,
    string DisplayName,
    string Description,
    decimal? MonthlyPriceCrc,
    decimal? AnnualPriceCrc,
    bool IsActive,
    string? CommercialApprovalReference,
    Guid? CommercialApprovedByUserId,
    DateTimeOffset? CommercialApprovedAt,
    bool IsCommerciallyApproved,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    Guid Version)
{
    public static SubscriptionPlanDto FromDomain(SubscriptionPlan plan) => new(
        plan.Id,
        plan.Tier,
        plan.DisplayName,
        plan.Description,
        plan.MonthlyPriceCrc,
        plan.AnnualPriceCrc,
        plan.IsActive,
        plan.CommercialApprovalReference,
        plan.CommercialApprovedByUserId,
        plan.CommercialApprovedAt,
        plan.IsCommerciallyApproved,
        plan.CreatedAt,
        plan.UpdatedAt,
        plan.Version);
}

/// <summary>Public plan fields; excludes approval reference and internal approver identity.</summary>
public sealed record PublicSubscriptionPlanDto(
    SubscriptionTier Tier,
    string DisplayName,
    string Description,
    decimal? MonthlyPriceCrc,
    decimal? AnnualPriceCrc)
{
    public static PublicSubscriptionPlanDto FromDomain(SubscriptionPlan plan) => new(
        plan.Tier,
        plan.DisplayName,
        plan.Description,
        plan.MonthlyPriceCrc,
        plan.AnnualPriceCrc);
}

public sealed record SubscriptionAddonDto(
    Guid Id,
    Guid SubscriptionId,
    string EntitlementKey,
    decimal Units,
    decimal? PriceCrc,
    DateTimeOffset StartsAt,
    DateTimeOffset ExpiresAt,
    bool IsActive)
{
    public static SubscriptionAddonDto FromDomain(SubscriptionAddon addon) => new(
        addon.Id, addon.SubscriptionId, addon.EntitlementKey, addon.Units, addon.PriceCrc,
        addon.StartsAt, addon.ExpiresAt, addon.IsActive);
}

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
        plan.CreatedAt,
        plan.UpdatedAt,
        plan.Version);
}
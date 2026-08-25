namespace PawTrack.Domain.Subscriptions;

/// <summary>
/// Single source of truth for paid subscription prices (CRC, monthly). Amounts are placeholders
/// pending final commercial approval (see docs/todolist-b2b-enterprise.md §0) — the goal of this
/// catalog is to remove hardcoded/scattered prices, not to lock in final figures.
/// </summary>
public static class SubscriptionPricing
{
    public static readonly IReadOnlyDictionary<SubscriptionTier, decimal> MonthlyPriceCrc =
        new Dictionary<SubscriptionTier, decimal>
        {
            [SubscriptionTier.UserPlus] = 2_990m,
            [SubscriptionTier.UserFamilia] = 4_990m,
            [SubscriptionTier.ClinicPlus] = 15_000m,
            [SubscriptionTier.ClinicPartner] = 35_000m,
            [SubscriptionTier.StorePlus] = 12_000m,
            [SubscriptionTier.StorePartner] = 25_000m,
            [SubscriptionTier.ShelterPlus] = 8_000m,
        };

    public static bool TryGetMonthlyPriceCrc(SubscriptionTier tier, out decimal amountCrc) =>
        MonthlyPriceCrc.TryGetValue(tier, out amountCrc);

    /// <summary>True for any tier that requires payment (i.e. not a free/base tier).</summary>
    public static bool IsPaidTier(SubscriptionTier tier) => MonthlyPriceCrc.ContainsKey(tier);
}

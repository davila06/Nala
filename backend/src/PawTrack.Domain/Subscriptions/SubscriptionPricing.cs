namespace PawTrack.Domain.Subscriptions;

/// <summary>
/// Single source of truth for paid subscription prices (CRC, monthly). Amounts are net base costs
/// that do NOT reflect the 13% Costa Rica IVA. When a client requires an electronic invoice
/// (Factura Electrónica con crédito fiscal), 13% IVA is added to the service cost.
/// </summary>
public static class SubscriptionPricing
{
    public const int AnnualTerm = 12;
    public const decimal AnnualDiscount = 0.20m;

    /// <summary>
    /// Costa Rica standard Value Added Tax (IVA) rate: 13%.
    /// </summary>
    public const decimal StandardIvaRate = 0.13m;

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

    // Municipal tiers are billed annually; keep separate to avoid mixing billing cycles.
    public static readonly IReadOnlyDictionary<SubscriptionTier, decimal> AnnualPriceCrc =
        new Dictionary<SubscriptionTier, decimal>
        {
            [SubscriptionTier.MuniBasica] = 150_000m,
            [SubscriptionTier.MuniFull] = 300_000m,
            [SubscriptionTier.MuniRedRegional] = 500_000m,
        };

    public static bool TryGetMonthlyPriceCrc(SubscriptionTier tier, out decimal amountCrc) =>
        MonthlyPriceCrc.TryGetValue(tier, out amountCrc);

    public static bool TryGetAnnualPriceCrc(SubscriptionTier tier, out decimal amountCrc) =>
        AnnualPriceCrc.TryGetValue(tier, out amountCrc);

    public static bool IsPaidTier(SubscriptionTier tier) =>
        MonthlyPriceCrc.ContainsKey(tier) || AnnualPriceCrc.ContainsKey(tier);

    public static bool IsMunicipalTier(SubscriptionTier tier) =>
        AnnualPriceCrc.ContainsKey(tier);

    public static bool IsUserTermTier(SubscriptionTier tier) =>
        tier is SubscriptionTier.UserPlus or SubscriptionTier.UserFamilia;

    public static bool IsSupportedBillingMonths(int billingMonths) =>
        billingMonths is 1 or 3 or 6 or AnnualTerm;

    public static decimal CalculateTermPriceCrc(decimal monthlyPriceCrc, int billingMonths)
    {
        if (monthlyPriceCrc <= 0)
            throw new ArgumentOutOfRangeException(nameof(monthlyPriceCrc));
        if (!IsSupportedBillingMonths(billingMonths))
            throw new ArgumentOutOfRangeException(nameof(billingMonths));

        var undiscountedAmount = monthlyPriceCrc * billingMonths;
        return billingMonths == AnnualTerm
            ? undiscountedAmount * (1 - AnnualDiscount)
            : undiscountedAmount;
    }

    /// <summary>
    /// Calculates the 13% IVA amount for a given base service cost.
    /// </summary>
    public static decimal CalculateIvaAmountCrc(decimal baseAmountCrc) =>
        Math.Round(baseAmountCrc * StandardIvaRate, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Calculates total amount with 13% IVA added to the base service cost.
    /// </summary>
    public static decimal CalculateTotalWithIvaCrc(decimal baseAmountCrc) =>
        Math.Round(baseAmountCrc * (1m + StandardIvaRate), 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Returns the final amount to charge: if client wants an invoice, the 13% IVA is added
    /// to the service cost; otherwise, the base service cost is returned.
    /// </summary>
    public static decimal GetEffectivePriceCrc(decimal baseAmountCrc, bool requiresInvoice) =>
        requiresInvoice ? CalculateTotalWithIvaCrc(baseAmountCrc) : baseAmountCrc;
}

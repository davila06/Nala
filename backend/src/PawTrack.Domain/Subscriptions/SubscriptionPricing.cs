namespace PawTrack.Domain.Subscriptions;

/// <summary>
/// Billing rules only. Plan prices are stored in SubscriptionPlans and must be read from the
/// catalog; this type does not define an authoritative price. Amounts are net base costs and do
/// not reflect Costa Rica IVA. When an invoice is required, 13% IVA is added to the service cost.
/// </summary>
public static class SubscriptionPricing
{
    public const int AnnualTerm = 12;
    public const decimal AnnualDiscount = 0.20m;

    /// <summary>
    /// Costa Rica standard Value Added Tax (IVA) rate: 13%.
    /// </summary>
    public const decimal StandardIvaRate = 0.13m;

    private static readonly IReadOnlySet<SubscriptionTier> PaidTiers = new HashSet<SubscriptionTier>
    {
        SubscriptionTier.UserPlus,
        SubscriptionTier.UserFamilia,
        SubscriptionTier.ClinicPlus,
        SubscriptionTier.ClinicPartner,
        SubscriptionTier.StorePlus,
        SubscriptionTier.StorePartner,
        SubscriptionTier.ShelterPlus,
        SubscriptionTier.MuniBasica,
        SubscriptionTier.MuniFull,
        SubscriptionTier.MuniRedRegional,
    };

    private static readonly IReadOnlySet<SubscriptionTier> MunicipalTiers = new HashSet<SubscriptionTier>
    {
        SubscriptionTier.MuniBasica,
        SubscriptionTier.MuniFull,
        SubscriptionTier.MuniRedRegional,
    };

    public static bool IsPaidTier(SubscriptionTier tier) => PaidTiers.Contains(tier);

    public static bool IsMunicipalTier(SubscriptionTier tier) => MunicipalTiers.Contains(tier);

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

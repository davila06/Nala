namespace PawTrack.Domain.Subscriptions;

public sealed class Subscription
{
    private Subscription() { } // EF Core

    public Guid Id { get; private set; }
    /// <summary>Owning user (pet owner) or null when this is a clinic subscription.</summary>
    public Guid? UserId { get; private set; }
    /// <summary>Owning clinic or null when this is a user subscription.</summary>
    public Guid? ClinicId { get; private set; }
    /// <summary>User who created a clinic subscription. Required for ownership checks (ClinicId != UserId).</summary>
    public Guid? ClinicOwnerId { get; private set; }
    public SubscriptionTier Tier { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    /// <summary>SINPE Móvil reference code (8 uppercase alphanum) shown to the subscriber.</summary>
    public string PaymentReference { get; private set; } = string.Empty;
    public decimal AmountCrc { get; private set; }
    /// <summary>Set when the subscriber self-reports having sent the SINPE payment.</summary>
    public DateTimeOffset? PaymentReportedAt { get; private set; }
    /// <summary>Set when this subscription was created via a promotion code.</summary>
    public Guid? RedeemedPromotionCodeId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ActivatedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }

    // ── Factories ─────────────────────────────────────────────────────────────

    public static Subscription CreateForUser(Guid userId, SubscriptionTier tier, string paymentReference, decimal amountCrc)
    {
        ValidateUserTier(tier);
        return new Subscription
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Tier = tier,
            Status = SubscriptionStatus.PendingPayment,
            PaymentReference = paymentReference,
            AmountCrc = amountCrc,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public static Subscription CreateForClinic(Guid clinicId, Guid ownerId, SubscriptionTier tier, string paymentReference, decimal amountCrc)
    {
        ValidateClinicTier(tier);
        return new Subscription
        {
            Id = Guid.CreateVersion7(),
            ClinicId = clinicId,
            ClinicOwnerId = ownerId,
            Tier = tier,
            Status = SubscriptionStatus.PendingPayment,
            PaymentReference = paymentReference,
            AmountCrc = amountCrc,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public static Subscription CreateFromPromotion(
        Guid userId, SubscriptionTier tier, int months, Guid promotionCodeId)
    {
        ValidateUserTier(tier);
        var now = DateTimeOffset.UtcNow;
        return new Subscription
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Tier = tier,
            Status = SubscriptionStatus.Active,
            PaymentReference = string.Empty,
            AmountCrc = 0,
            RedeemedPromotionCodeId = promotionCodeId,
            CreatedAt = now,
            ActivatedAt = now,
            ExpiresAt = now.AddMonths(months),
        };
    }

    // ── Behaviour ─────────────────────────────────────────────────────────────

    public void Activate(int billingMonths = 1)
    {
        if (Status != SubscriptionStatus.PendingPayment)
            throw new InvalidOperationException("Only pending subscriptions can be activated.");

        Status = SubscriptionStatus.Active;
        ActivatedAt = DateTimeOffset.UtcNow;
        ExpiresAt = DateTimeOffset.UtcNow.AddMonths(billingMonths);
    }

    public void Cancel()
    {
        if (Status != SubscriptionStatus.Active)
            throw new InvalidOperationException("Only active subscriptions can be cancelled.");

        Status = SubscriptionStatus.Cancelled;
        CancelledAt = DateTimeOffset.UtcNow;
    }

    public void MarkExpired()
    {
        Status = SubscriptionStatus.Expired;
    }

    /// <summary>Subscriber self-reports that they sent the SINPE payment.</summary>
    public void ReportPaymentSent()
    {
        if (Status != SubscriptionStatus.PendingPayment)
            throw new InvalidOperationException("Only pending subscriptions can have payment reported.");
        PaymentReportedAt = DateTimeOffset.UtcNow;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public bool IsActive => Status == SubscriptionStatus.Active && ExpiresAt > DateTimeOffset.UtcNow;

    private static void ValidateUserTier(SubscriptionTier tier)
    {
        if (tier is not (
            SubscriptionTier.UserPlus or
            SubscriptionTier.UserFamilia or
            SubscriptionTier.ShelterPlus))
            throw new ArgumentException($"Tier {tier} is not a valid user tier.");
    }

    private static void ValidateClinicTier(SubscriptionTier tier)
    {
        if (tier is not (SubscriptionTier.ClinicPlus or SubscriptionTier.ClinicPartner))
            throw new ArgumentException($"Tier {tier} is not a valid clinic tier.");
    }
}

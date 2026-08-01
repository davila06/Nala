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

    // ── Helpers ───────────────────────────────────────────────────────────────

    public bool IsActive => Status == SubscriptionStatus.Active && ExpiresAt > DateTimeOffset.UtcNow;

    private static void ValidateUserTier(SubscriptionTier tier)
    {
        if (tier is not (SubscriptionTier.UserPlus or SubscriptionTier.UserFamilia))
            throw new ArgumentException($"Tier {tier} is not a valid user tier.");
    }

    private static void ValidateClinicTier(SubscriptionTier tier)
    {
        if (tier is not (SubscriptionTier.ClinicPlus or SubscriptionTier.ClinicPartner))
            throw new ArgumentException($"Tier {tier} is not a valid clinic tier.");
    }
}

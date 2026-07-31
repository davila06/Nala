namespace PawTrack.Domain.Bounties;

public sealed class Bounty
{
    private Bounty() { } // EF Core

    public Guid          Id               { get; private set; }
    public Guid          LostPetEventId   { get; private set; }
    public Guid          OwnerId          { get; private set; }
    public decimal       Amount           { get; private set; }
    public string        CurrencyCode     { get; private set; } = "CRC";
    public BountyStatus  Status           { get; private set; }
    /// <summary>SINPE deposit reference generated for the owner.</summary>
    public string        DepositReference { get; private set; } = string.Empty;
    /// <summary>Platform commission fee (0–1) deducted before releasing to rescuer.</summary>
    public decimal       PlatformFee      { get; private set; }
    public Guid?         ClaimedBySightingId { get; private set; }
    public Guid?         ClaimedByUserId     { get; private set; }
    public DateTimeOffset  CreatedAt       { get; private set; }
    public DateTimeOffset? DepositedAt     { get; private set; }
    public DateTimeOffset? ClaimedAt       { get; private set; }
    public DateTimeOffset? ReleasedAt      { get; private set; }

    public decimal NetPayoutAmount => Amount * (1m - PlatformFee);

    // ── Factory ────────────────────────────────────────────────────────────────

    public static Bounty Create(
        Guid lostPetEventId,
        Guid ownerId,
        decimal amount,
        string depositReference,
        decimal platformFee = 0.10m,
        string currencyCode = "CRC")
    {
        if (amount <= 0) throw new ArgumentException("Amount must be positive.");
        if (platformFee is < 0 or > 0.5m) throw new ArgumentOutOfRangeException(nameof(platformFee));

        return new Bounty
        {
            Id               = Guid.CreateVersion7(),
            LostPetEventId   = lostPetEventId,
            OwnerId          = ownerId,
            Amount           = amount,
            CurrencyCode     = currencyCode,
            Status           = BountyStatus.PendingDeposit,
            DepositReference = depositReference,
            PlatformFee      = platformFee,
            CreatedAt        = DateTimeOffset.UtcNow,
        };
    }

    // ── Domain behaviour ───────────────────────────────────────────────────────

    public void ConfirmDeposit()
    {
        if (Status != BountyStatus.PendingDeposit)
            throw new InvalidOperationException("Bounty deposit already confirmed.");

        Status      = BountyStatus.Active;
        DepositedAt = DateTimeOffset.UtcNow;
    }

    public void Claim(Guid sightingId, Guid claimedByUserId)
    {
        if (Status != BountyStatus.Active)
            throw new InvalidOperationException("Only active bounties can be claimed.");

        Status                = BountyStatus.Claimed;
        ClaimedBySightingId   = sightingId;
        ClaimedByUserId       = claimedByUserId;
        ClaimedAt             = DateTimeOffset.UtcNow;
    }

    public void Release()
    {
        if (Status != BountyStatus.Claimed)
            throw new InvalidOperationException("Only claimed bounties can be released.");

        Status     = BountyStatus.Released;
        ReleasedAt = DateTimeOffset.UtcNow;
    }

    public void Refund()
    {
        if (Status is BountyStatus.Released or BountyStatus.Expired)
            throw new InvalidOperationException("Cannot refund a released or expired bounty.");

        Status = BountyStatus.Refunded;
    }

    public void Expire()
    {
        if (Status is BountyStatus.Released or BountyStatus.Claimed)
            return; // already concluded

        Status = BountyStatus.Expired;
    }
}

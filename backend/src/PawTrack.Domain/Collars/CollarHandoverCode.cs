namespace PawTrack.Domain.Collars;

/// <summary>
/// A one-time PIN that authorizes transferring an activated PawTrack collar (with a
/// physical serial) from its current owner to a new owner — analogous to Apple's
/// AirTag "remove owner" transfer flow. Redemption releases the serial back to
/// <see cref="CollarTagStatus.Unactivated"/>; the new owner completes onboarding
/// through the existing serial-activation flow (<c>ActivateCollarTagCommand</c>),
/// which already enforces plan requirements and issues a fresh device credential.
/// </summary>
public sealed class CollarHandoverCode
{
    public const int MaxAttempts = 5;

    private CollarHandoverCode() { } // EF Core

    public Guid Id { get; private set; }
    public Guid CollarId { get; private set; }
    public Guid GeneratedByOwnerId { get; private set; }
    /// <summary>SHA-256 hex hash of the 6-digit PIN. Never stored in plain text — valid for 7 days.</summary>
    public string PinHash { get; private set; } = string.Empty;
    public int AttemptCount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RedeemedAt { get; private set; }
    public Guid? RedeemedByUserId { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }

    public bool IsRedeemed => RedeemedAt.HasValue;
    public bool IsCancelled => CancelledAt.HasValue;
    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt;
    public bool IsLocked => AttemptCount >= MaxAttempts;
    public bool IsRedeemable => !IsRedeemed && !IsCancelled && !IsExpired && !IsLocked;

    public static CollarHandoverCode Create(Guid collarId, Guid generatedByOwnerId, string pinHash)
    {
        var now = DateTimeOffset.UtcNow;
        return new CollarHandoverCode
        {
            Id = Guid.CreateVersion7(),
            CollarId = collarId,
            GeneratedByOwnerId = generatedByOwnerId,
            PinHash = pinHash,
            AttemptCount = 0,
            CreatedAt = now,
            ExpiresAt = now.AddDays(7),
        };
    }

    public void RecordFailedAttempt() => AttemptCount++;

    public void Redeem(Guid redeemedByUserId)
    {
        RedeemedAt = DateTimeOffset.UtcNow;
        RedeemedByUserId = redeemedByUserId;
    }

    public void Cancel() => CancelledAt = DateTimeOffset.UtcNow;
}

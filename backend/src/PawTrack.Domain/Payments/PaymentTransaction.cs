namespace PawTrack.Domain.Payments;

public enum PaymentTransactionStatus
{
    Pending = 1,
    Succeeded = 2,
    Failed = 3,
}

/// <summary>
/// Immutable audit and idempotency record of all card charges and automated gateway payment attempts.
/// </summary>
public sealed class PaymentTransaction
{
    private PaymentTransaction() { } // EF Core

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? PaymentProfileId { get; private set; }
    public decimal AmountCrc { get; private set; }
    public string Currency { get; private set; } = "CRC";
    public string TransactionReference { get; private set; } = string.Empty;
    public string? GatewayAuthorizationCode { get; private set; }
    public string Purpose { get; private set; } = string.Empty; // "Subscription", "BundleOrder", "Bounty"
    public Guid? TargetEntityId { get; private set; }
    public PaymentTransactionStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public static PaymentTransaction Record(
        Guid userId,
        decimal amountCrc,
        string transactionReference,
        string purpose,
        Guid? paymentProfileId = null,
        Guid? targetEntityId = null,
        string currency = "CRC")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        return new PaymentTransaction
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            PaymentProfileId = paymentProfileId,
            AmountCrc = amountCrc,
            Currency = currency.ToUpperInvariant(),
            TransactionReference = transactionReference.Trim(),
            Purpose = purpose.Trim(),
            TargetEntityId = targetEntityId,
            Status = PaymentTransactionStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void MarkSucceeded(string? authCode = null)
    {
        Status = PaymentTransactionStatus.Succeeded;
        GatewayAuthorizationCode = authCode;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        Status = PaymentTransactionStatus.Failed;
        FailureReason = reason;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}

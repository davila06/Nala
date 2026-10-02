using PawTrack.Domain.Common;
using PawTrack.Domain.Payments.Events;

namespace PawTrack.Domain.Payments;

public enum PaymentIntentStatus
{
    Created = 1,
    PendingCustomerAction = 2,
    Authorized = 3,
    Captured = 4,
    Settled = 5,
    Failed = 6,
    Declined = 7,
    Cancelled = 8,
    Refunded = 9,
    PartiallyRefunded = 10,
    Disputed = 11,
    Unknown = 12,
}

public sealed class PaymentIntent : IHasDomainEvents
{
    private readonly List<object> _domainEvents = [];
    private PaymentIntent() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public decimal AmountCrc { get; private set; }
    public string Currency { get; private set; } = "CRC";
    public string MerchantReference { get; private set; } = string.Empty;
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string Purpose { get; private set; } = string.Empty;
    public Guid? TargetEntityId { get; private set; }
    public PaymentIntentStatus Status { get; private set; }
    public string? GatewayTransactionId { get; private set; }
    public string? AuthorizationCode { get; private set; }
    public string? FailureReason { get; private set; }
    public decimal CapturedAmountCrc { get; private set; }
    public decimal RefundedAmountCrc { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();

    public bool IsSuccessful => Status is
        PaymentIntentStatus.Authorized or
        PaymentIntentStatus.Captured or
        PaymentIntentStatus.Settled or
        PaymentIntentStatus.PartiallyRefunded or
        PaymentIntentStatus.Refunded;

    public static PaymentIntent Create(
        Guid userId,
        decimal amountCrc,
        string currency,
        string merchantReference,
        string idempotencyKey,
        string purpose,
        Guid? targetEntityId = null)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User is required.", nameof(userId));
        if (amountCrc <= 0) throw new ArgumentOutOfRangeException(nameof(amountCrc));
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentException.ThrowIfNullOrWhiteSpace(merchantReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        var now = DateTimeOffset.UtcNow;
        return new PaymentIntent
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            AmountCrc = decimal.Round(amountCrc, 2, MidpointRounding.ToEven),
            Currency = currency.Trim().ToUpperInvariant(),
            MerchantReference = merchantReference.Trim(),
            IdempotencyKey = idempotencyKey.Trim(),
            Purpose = purpose.Trim(),
            TargetEntityId = targetEntityId,
            Status = PaymentIntentStatus.Created,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void MarkPendingCustomerAction()
    {
        TransitionFrom(PaymentIntentStatus.Created);
        Status = PaymentIntentStatus.PendingCustomerAction;
        Touch();
    }

    public void MarkAuthorized(string gatewayTransactionId, string? authorizationCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayTransactionId);
        TransitionFrom(PaymentIntentStatus.PendingCustomerAction);
        GatewayTransactionId = gatewayTransactionId.Trim();
        AuthorizationCode = authorizationCode?.Trim();
        Status = PaymentIntentStatus.Authorized;
        Touch();
    }

    public void MarkCaptured()
    {
        TransitionFrom(PaymentIntentStatus.Authorized);
        CapturedAmountCrc = AmountCrc;
        Status = PaymentIntentStatus.Captured;
        Touch();
    }

    public void MarkSettled(
        string? gatewayTransactionId = null,
        Guid? paymentOperationId = null,
        string? correlationId = null)
    {
        TransitionFrom(PaymentIntentStatus.Captured);
        Status = PaymentIntentStatus.Settled;
        Touch();
        _domainEvents.Add(new PaymentIntentSettledDomainEvent(
            Id, UserId, Purpose, TargetEntityId, AmountCrc,
            gatewayTransactionId, paymentOperationId, correlationId));
    }

    public void MarkFailed(string reason) => MarkTerminalFailure(PaymentIntentStatus.Failed, reason);

    public void MarkDeclined(string reason) => MarkTerminalFailure(PaymentIntentStatus.Declined, reason);

    public void MarkCancelled(string reason = "Cancelled") => MarkTerminalFailure(PaymentIntentStatus.Cancelled, reason);

    public void MarkUnknown(string reason) => MarkTerminalFailure(PaymentIntentStatus.Unknown, reason);

    public void MarkDisputed(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status is not (PaymentIntentStatus.Settled or PaymentIntentStatus.PartiallyRefunded))
            throw new InvalidOperationException($"Cannot dispute payment in status {Status}.");

        Status = PaymentIntentStatus.Disputed;
        FailureReason = reason.Trim();
        Touch();
    }

    public void ApplyRefund(decimal amountCrc)
    {
        if (amountCrc <= 0) throw new ArgumentOutOfRangeException(nameof(amountCrc));
        if (Status is not (PaymentIntentStatus.Settled or PaymentIntentStatus.PartiallyRefunded))
            throw new InvalidOperationException($"Cannot refund payment in status {Status}.");

        var nextRefundedAmount = RefundedAmountCrc + decimal.Round(amountCrc, 2, MidpointRounding.ToEven);
        if (nextRefundedAmount > CapturedAmountCrc)
            throw new InvalidOperationException("Refund amount exceeds captured amount.");

        RefundedAmountCrc = nextRefundedAmount;
        Status = RefundedAmountCrc == CapturedAmountCrc
            ? PaymentIntentStatus.Refunded
            : PaymentIntentStatus.PartiallyRefunded;
        Touch();
    }

    private void MarkTerminalFailure(PaymentIntentStatus status, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (Status is PaymentIntentStatus.Settled or
            PaymentIntentStatus.Captured or
            PaymentIntentStatus.Refunded or
            PaymentIntentStatus.PartiallyRefunded or
            PaymentIntentStatus.Disputed)
            throw new InvalidOperationException($"Cannot transition payment from status {Status} to {status}.");

        Status = status;
        FailureReason = reason.Trim();
        Touch();
    }

    private void TransitionFrom(PaymentIntentStatus expected)
    {
        if (Status != expected)
            throw new InvalidOperationException($"Expected payment status {expected}, actual status {Status}.");
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    public void ClearDomainEvents() => _domainEvents.Clear();
}

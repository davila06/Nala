namespace PawTrack.Domain.Payments;

public enum PaymentOperationType
{
    Capture = 1,
    Void = 2,
    Refund = 3,
    WebhookReceived = 4,
    Reconciliation = 5,
}

public enum PaymentOperationStatus
{
    Processing = 1,
    Succeeded = 2,
    Failed = 3,
    Unknown = 4,
    Ignored = 5,
}

public sealed class PaymentOperation
{
    private PaymentOperation() { }

    public Guid Id { get; private set; }
    public Guid? PaymentIntentId { get; private set; }
    public PaymentOperationType OperationType { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string RequestHash { get; private set; } = string.Empty;
    public string? ProviderOperationId { get; private set; }
    public PaymentOperationStatus Status { get; private set; }
    public string? ResponseJson { get; private set; }
    public string? FailureReason { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public static PaymentOperation Create(
        Guid? paymentIntentId,
        PaymentOperationType operationType,
        string idempotencyKey,
        string requestHash,
        string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        var now = DateTimeOffset.UtcNow;
        return new PaymentOperation
        {
            Id = Guid.CreateVersion7(),
            PaymentIntentId = paymentIntentId,
            OperationType = operationType,
            IdempotencyKey = idempotencyKey.Trim(),
            RequestHash = requestHash.Trim(),
            CorrelationId = correlationId.Trim(),
            Status = PaymentOperationStatus.Processing,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void MarkSucceeded(string? providerOperationId, string responseJson)
    {
        if (Status != PaymentOperationStatus.Processing) return;
        ProviderOperationId = providerOperationId?.Trim();
        ResponseJson = responseJson;
        FailureReason = null;
        Status = PaymentOperationStatus.Succeeded;
        Complete();
    }

    public void MarkFailed(string reason, string? responseJson = null)
    {
        if (Status != PaymentOperationStatus.Processing) return;
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        FailureReason = reason.Trim();
        ResponseJson = responseJson;
        Status = PaymentOperationStatus.Failed;
        Complete();
    }

    public void MarkUnknown(string reason, string? responseJson = null)
    {
        if (Status != PaymentOperationStatus.Processing) return;
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        FailureReason = reason.Trim();
        ResponseJson = responseJson;
        Status = PaymentOperationStatus.Unknown;
        Complete();
    }

    public void MarkIgnored(string responseJson)
    {
        if (Status != PaymentOperationStatus.Processing) return;
        ResponseJson = responseJson;
        Status = PaymentOperationStatus.Ignored;
        Complete();
    }

    private void Complete()
    {
        CompletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CompletedAt.Value;
    }
}

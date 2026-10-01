namespace PawTrack.Domain.Payments;

public enum PaymentLedgerEntryType
{
    Debit = 1,
    Credit = 2,
    Refund = 3,
}

public sealed class PaymentLedgerEntry
{
    private PaymentLedgerEntry() { }

    public Guid Id { get; private set; }
    public Guid PaymentIntentId { get; private set; }
    public Guid PaymentOperationId { get; private set; }
    public PaymentLedgerEntryType EntryType { get; private set; }
    public decimal AmountCrc { get; private set; }
    public string Currency { get; private set; } = "CRC";
    public string Reference { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public static PaymentLedgerEntry Create(
        Guid paymentIntentId,
        Guid paymentOperationId,
        PaymentLedgerEntryType entryType,
        decimal amountCrc,
        string currency,
        string reference,
        string correlationId)
    {
        if (paymentIntentId == Guid.Empty) throw new ArgumentException("Payment intent is required.", nameof(paymentIntentId));
        if (paymentOperationId == Guid.Empty) throw new ArgumentException("Payment operation is required.", nameof(paymentOperationId));
        if (amountCrc <= 0) throw new ArgumentOutOfRangeException(nameof(amountCrc));
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentException.ThrowIfNullOrWhiteSpace(reference);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        return new PaymentLedgerEntry
        {
            Id = Guid.CreateVersion7(),
            PaymentIntentId = paymentIntentId,
            PaymentOperationId = paymentOperationId,
            EntryType = entryType,
            AmountCrc = decimal.Round(amountCrc, 2, MidpointRounding.ToEven),
            Currency = currency.Trim().ToUpperInvariant(),
            Reference = reference.Trim(),
            CorrelationId = correlationId.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}

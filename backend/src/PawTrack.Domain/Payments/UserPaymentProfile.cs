namespace PawTrack.Domain.Payments;

/// <summary>
/// Stored payment profile for recurring billing and 1-click checkouts.
/// Adheres strictly to PCI-DSS SAQ A: raw PAN and CVV are NEVER stored or processed on PawTrack servers.
/// Only provider tokenization IDs and masked display metadata (brand, last4, expiry) are retained.
/// </summary>
public sealed class UserPaymentProfile
{
    private UserPaymentProfile() { } // EF Core

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public PaymentMethodType MethodType { get; private set; }
    public string ProviderToken { get; private set; } = string.Empty;
    public string? CardBrand { get; private set; }
    public string? LastFourDigits { get; private set; }
    public int? ExpirationMonth { get; private set; }
    public int? ExpirationYear { get; private set; }
    public string? CardholderName { get; private set; }
    public bool IsDefault { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }

    public static UserPaymentProfile CreateCard(
        Guid userId,
        string providerToken,
        string cardBrand,
        string lastFourDigits,
        int? expMonth,
        int? expYear,
        string? cardholderName = null,
        bool isDefault = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(cardBrand);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastFourDigits);

        return new UserPaymentProfile
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            MethodType = PaymentMethodType.CreditDebitCard,
            ProviderToken = providerToken.Trim(),
            CardBrand = cardBrand.Trim(),
            LastFourDigits = lastFourDigits.Trim(),
            ExpirationMonth = expMonth,
            ExpirationYear = expYear,
            CardholderName = string.IsNullOrWhiteSpace(cardholderName) ? null : cardholderName.Trim(),
            IsDefault = isDefault,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void SetDefault(bool isDefault) => IsDefault = isDefault;

    public void RecordUsage() => LastUsedAt = DateTimeOffset.UtcNow;
}

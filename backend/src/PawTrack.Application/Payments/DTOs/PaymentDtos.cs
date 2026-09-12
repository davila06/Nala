using PawTrack.Domain.Payments;

namespace PawTrack.Application.Payments.DTOs;

public sealed record PaymentProfileDto(
    Guid Id,
    Guid UserId,
    string MethodType,
    string CardBrand,
    string LastFourDigits,
    int? ExpirationMonth,
    int? ExpirationYear,
    string? CardholderName,
    bool IsDefault,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt)
{
    public static PaymentProfileDto FromDomain(UserPaymentProfile p) => new(
        p.Id,
        p.UserId,
        p.MethodType.ToString(),
        p.CardBrand ?? "Card",
        p.LastFourDigits ?? "****",
        p.ExpirationMonth,
        p.ExpirationYear,
        p.CardholderName,
        p.IsDefault,
        p.CreatedAt,
        p.LastUsedAt);
}

public sealed record CaptureContextDto(
    string ClientLibraryUrl,
    string CaptureContextJwt,
    string KeyId,
    bool IsConfigured);

public sealed record ChargeCardResultDto(
    bool Success,
    string TransactionReference,
    string? AuthorizationCode,
    string? ErrorMessage,
    Guid? ActivatedSubscriptionId = null,
    Guid? ConfirmedBundleOrderId = null);

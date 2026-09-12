namespace PawTrack.Application.Payments.Interfaces;

public sealed record CaptureContextResult(
    string ClientLibraryUrl,
    string CaptureContextJwt,
    string KeyId,
    bool IsConfigured);

public sealed record TokenizePaymentRequest(
    string TransientToken,
    string? CardholderName = null);

public sealed record TokenizePaymentResult(
    bool Success,
    string? CustomerProfileId,
    string? PaymentInstrumentId,
    string? CardBrand,
    string? LastFourDigits,
    int? ExpirationMonth,
    int? ExpirationYear,
    string? ErrorMessage);

public sealed record ChargePaymentRequest(
    decimal AmountCrc,
    string PaymentInstrumentOrCustomerToken,
    string OrderReference,
    string Purpose,
    string? CardholderEmail = null,
    string Currency = "CRC");

public sealed record ChargePaymentResult(
    bool Success,
    string? GatewayTransactionId,
    string? AuthorizationCode,
    string? ErrorCode,
    string? ErrorMessage);

public interface IPaymentGatewayService
{
    bool IsConfigured { get; }

    Task<CaptureContextResult> GenerateCaptureContextAsync(
        CancellationToken cancellationToken = default);

    Task<TokenizePaymentResult> TokenizeTransientTokenAsync(
        TokenizePaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<ChargePaymentResult> ChargeAsync(
        ChargePaymentRequest request,
        CancellationToken cancellationToken = default);
}

namespace PawTrack.Application.ServiceProviders.Payments;

public enum ProviderPaymentIntentStatus
{
    Pending,
    RequiresAction,
    Authorized,
    Failed,
}

public sealed record ProviderPaymentIntentRequest(
    Guid BookingId,
    decimal AmountCrc,
    string Currency,
    string PaymentReference,
    string IdempotencyKey);

public sealed record ProviderPaymentIntentResult(
    ProviderPaymentIntentStatus Status,
    decimal AmountCrc,
    string Currency,
    string PaymentReference,
    string? ExternalIntentId,
    string? FailureReason);

public interface IProviderPaymentGateway
{
    Task<ProviderPaymentIntentResult> CreateIntentAsync(
        ProviderPaymentIntentRequest request,
        CancellationToken cancellationToken = default);
}

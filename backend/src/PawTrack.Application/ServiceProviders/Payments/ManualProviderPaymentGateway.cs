namespace PawTrack.Application.ServiceProviders.Payments;

public sealed class ManualProviderPaymentGateway : IProviderPaymentGateway
{
    public Task<ProviderPaymentIntentResult> CreateIntentAsync(
        ProviderPaymentIntentRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new ProviderPaymentIntentResult(
            ProviderPaymentIntentStatus.Pending,
            request.AmountCrc,
            request.Currency.Trim().ToUpperInvariant(),
            request.PaymentReference,
            ExternalIntentId: null,
            FailureReason: null));
}

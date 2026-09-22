using PawTrack.Application.Payments.Interfaces;

namespace PawTrack.IntegrationTests.Infrastructure;

public sealed class CapturingPaymentGateway : IPaymentGatewayService
{
    public bool IsConfigured => true;
    public List<ChargePaymentRequest> Charges { get; } = [];

    public void Clear() => Charges.Clear();

    public Task<CaptureContextResult> GenerateCaptureContextAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new CaptureContextResult(string.Empty, string.Empty, "test", true));

    public Task<TokenizePaymentResult> TokenizeTransientTokenAsync(
        TokenizePaymentRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new TokenizePaymentResult(true, "customer", "instrument", "Visa", "4242", 12, 2030, null));

    public Task<ChargePaymentResult> ChargeAsync(
        ChargePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        Charges.Add(request);
        return Task.FromResult(new ChargePaymentResult(true, "gateway-test", "auth-test", null, null));
    }
}

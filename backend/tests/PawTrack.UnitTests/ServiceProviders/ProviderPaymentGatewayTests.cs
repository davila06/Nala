using FluentAssertions;
using PawTrack.Application.ServiceProviders;
using PawTrack.Application.ServiceProviders.Payments;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class ProviderPaymentGatewayTests
{
    [Fact]
    public async Task ManualGateway_CreatesPendingIntentWithoutExternalSideEffects()
    {
        var gateway = new ManualProviderPaymentGateway();
        var request = new ProviderPaymentIntentRequest(
            Guid.NewGuid(),
            25_000m,
            "CRC",
            "PAY-123",
            "idem-123");

        var result = await gateway.CreateIntentAsync(request);

        result.Status.Should().Be(ProviderPaymentIntentStatus.Pending);
        result.AmountCrc.Should().Be(25_000m);
        result.Currency.Should().Be("CRC");
        result.ExternalIntentId.Should().BeNull();
    }
}

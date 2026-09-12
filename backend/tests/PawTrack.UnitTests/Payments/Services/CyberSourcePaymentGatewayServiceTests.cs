using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Infrastructure.Payments;

namespace PawTrack.UnitTests.Payments.Services;

public sealed class CyberSourcePaymentGatewayServiceTests
{
    [Fact]
    public async Task GenerateCaptureContextAsync_WhenNotConfigured_ReturnsSimulatedContext()
    {
        var config = new ConfigurationBuilder().Build();
        var httpFactory = Substitute.For<IHttpClientFactory>();
        var sut = new CyberSourcePaymentGatewayService(httpFactory, config, NullLogger<CyberSourcePaymentGatewayService>.Instance);

        var result = await sut.GenerateCaptureContextAsync();

        result.IsConfigured.Should().BeFalse();
        result.ClientLibraryUrl.Should().Contain("flex-microform");
        result.CaptureContextJwt.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task TokenizeTransientTokenAsync_WhenNotConfigured_SimulatesTokenization()
    {
        var config = new ConfigurationBuilder().Build();
        var httpFactory = Substitute.For<IHttpClientFactory>();
        var sut = new CyberSourcePaymentGatewayService(httpFactory, config, NullLogger<CyberSourcePaymentGatewayService>.Instance);

        var result = await sut.TokenizeTransientTokenAsync(new TokenizePaymentRequest("temp-token-xyz", "CARLOS ROJAS"));

        result.Success.Should().BeTrue();
        result.CardBrand.Should().Be("Visa");
        result.LastFourDigits.Should().HaveLength(4);
        result.PaymentInstrumentId.Should().StartWith("tok_");
    }

    [Fact]
    public async Task ChargeAsync_WhenValidAmount_ReturnsSuccessfulAuthorization()
    {
        var config = new ConfigurationBuilder().Build();
        var httpFactory = Substitute.For<IHttpClientFactory>();
        var sut = new CyberSourcePaymentGatewayService(httpFactory, config, NullLogger<CyberSourcePaymentGatewayService>.Instance);

        var result = await sut.ChargeAsync(new ChargePaymentRequest(
            AmountCrc: 4990m,
            PaymentInstrumentOrCustomerToken: "tok_12345678",
            OrderReference: "ORDER-101",
            Purpose: "Subscription"));

        result.Success.Should().BeTrue();
        result.AuthorizationCode.Should().StartWith("AUTH-");
        result.GatewayTransactionId.Should().StartWith("CS-");
    }

    [Fact]
    public async Task ChargeAsync_WhenAmountIsZeroOrNegative_ReturnsFailure()
    {
        var config = new ConfigurationBuilder().Build();
        var httpFactory = Substitute.For<IHttpClientFactory>();
        var sut = new CyberSourcePaymentGatewayService(httpFactory, config, NullLogger<CyberSourcePaymentGatewayService>.Instance);

        var result = await sut.ChargeAsync(new ChargePaymentRequest(
            AmountCrc: -10m,
            PaymentInstrumentOrCustomerToken: "tok_12345678",
            OrderReference: "ORDER-101",
            Purpose: "Subscription"));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_AMOUNT");
    }
}

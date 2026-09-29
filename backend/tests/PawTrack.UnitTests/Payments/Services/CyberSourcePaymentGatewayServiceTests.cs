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
    public async Task GenerateCaptureContextAsync_WhenNotConfigured_ReturnsUnavailableContext()
    {
        var config = new ConfigurationBuilder().Build();
        var httpFactory = Substitute.For<IHttpClientFactory>();
        var sut = new CyberSourcePaymentGatewayService(httpFactory, config, NullLogger<CyberSourcePaymentGatewayService>.Instance);

        var result = await sut.GenerateCaptureContextAsync();

        result.IsConfigured.Should().BeFalse();
        result.ClientLibraryUrl.Should().Contain("flex-microform");
        result.CaptureContextJwt.Should().BeEmpty();
        result.KeyId.Should().BeEmpty();
    }

    [Fact]
    public async Task TokenizeTransientTokenAsync_WhenNotConfigured_FailsClosed()
    {
        var config = new ConfigurationBuilder().Build();
        var httpFactory = Substitute.For<IHttpClientFactory>();
        var sut = new CyberSourcePaymentGatewayService(httpFactory, config, NullLogger<CyberSourcePaymentGatewayService>.Instance);

        var result = await sut.TokenizeTransientTokenAsync(new TokenizePaymentRequest("temp-token-xyz", "CARLOS ROJAS"));

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("La pasarela de pagos no está configurada.");
    }

    [Fact]
    public async Task ChargeAsync_WhenNotConfigured_FailsClosed()
    {
        var config = new ConfigurationBuilder().Build();
        var httpFactory = Substitute.For<IHttpClientFactory>();
        var sut = new CyberSourcePaymentGatewayService(httpFactory, config, NullLogger<CyberSourcePaymentGatewayService>.Instance);

        var result = await sut.ChargeAsync(new ChargePaymentRequest(
            AmountCrc: 4990m,
            PaymentInstrumentOrCustomerToken: "tok_12345678",
            OrderReference: "ORDER-101",
            Purpose: "Subscription"));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("PROVIDER_NOT_CONFIGURED");
        result.ErrorMessage.Should().Be("La pasarela de pagos no está configurada.");
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

    [Theory]
    [InlineData("CaptureAsync")]
    [InlineData("VoidAsync")]
    [InlineData("RefundAsync")]
    public async Task Financial_operations_WhenNotConfigured_FailClosed(string operation)
    {
        var config = new ConfigurationBuilder().Build();
        var httpFactory = Substitute.For<IHttpClientFactory>();
        var sut = new CyberSourcePaymentGatewayService(httpFactory, config, NullLogger<CyberSourcePaymentGatewayService>.Instance);
        var request = new PaymentOperationRequest("cs-txn-1", 4990m, "PT-ORDER-1");

        var result = operation switch
        {
            "CaptureAsync" => await sut.CaptureAsync(request),
            "VoidAsync" => await sut.VoidAsync(request),
            _ => await sut.RefundAsync(request),
        };

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("PROVIDER_NOT_CONFIGURED");
    }
}

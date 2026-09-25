using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using PawTrack.Infrastructure.Clinics;

namespace PawTrack.UnitTests.Clinics;

public sealed class ClinicEmailGatewayTests
{
    [Fact]
    public async Task MissingCredentials_FailsClosed()
    {
        var handler = new StubHandler(HttpStatusCode.Accepted, "provider-123");
        var gateway = new SendGridClinicEmailGateway(new FixedClientFactory(handler),
            new ConfigurationBuilder().Build());

        var result = await gateway.SendAsync("owner@test.cr", "Seguimiento", "Hola", Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        handler.Calls.Should().Be(0);
    }

    [Fact]
    public async Task ProviderAcceptance_RequiresReceiptHeader()
    {
        var handler = new StubHandler(HttpStatusCode.Accepted, null);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SendGrid:ApiKey"] = "test-key",
            ["SendGrid:FromEmail"] = "clinic@pawtrack.cr"
        }).Build();
        var gateway = new SendGridClinicEmailGateway(new FixedClientFactory(handler), config);

        var result = await gateway.SendAsync("owner@test.cr", "Seguimiento", "Hola", Guid.NewGuid(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        handler.Calls.Should().Be(1);
    }

    [Fact]
    public async Task ProviderReceipt_ConfirmsSubmissionOnly()
    {
        var handler = new StubHandler(HttpStatusCode.Accepted, "sg-123");
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SendGrid:ApiKey"] = "test-key",
            ["SendGrid:FromEmail"] = "clinic@pawtrack.cr"
        }).Build();
        var gateway = new SendGridClinicEmailGateway(new FixedClientFactory(handler), config);

        var result = await gateway.SendAsync("owner@test.cr", "Seguimiento", "Hola", Guid.NewGuid(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("sg-123");
    }

    private sealed class FixedClientFactory(StubHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class StubHandler(HttpStatusCode status, string? receipt) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            var response = new HttpResponseMessage(status);
            if (receipt is not null) response.Headers.Add("X-Message-Id", receipt);
            return Task.FromResult(response);
        }
    }
}

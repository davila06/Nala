using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using PawTrack.Infrastructure.Clinics;

namespace PawTrack.UnitTests.Clinics;

public sealed class ClinicFiscalGatewayTests
{
    [Fact]
    public async Task MissingProviderCredentials_FailsWithoutRequest()
    {
        var handler = new StubHandler(HttpStatusCode.Accepted, "fiscal-123");
        var gateway = new ConfiguredClinicFiscalGateway(new FixedClientFactory(handler), new ConfigurationBuilder().Build());

        var result = await gateway.SubmitAsync(Guid.NewGuid(), Guid.NewGuid(), "3101111111", "REC-1", 1000m, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        handler.Calls.Should().Be(0);
    }

    [Fact]
    public async Task ProviderMustReturnAcceptedAndDocumentReference()
    {
        var handler = new StubHandler(HttpStatusCode.Accepted, null);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ClinicFiscal:ProviderUrl"] = "https://fiscal.example.test/invoices",
            ["ClinicFiscal:ApiToken"] = "test-token"
        }).Build();
        var gateway = new ConfiguredClinicFiscalGateway(new FixedClientFactory(handler), configuration);

        var result = await gateway.SubmitAsync(Guid.NewGuid(), Guid.NewGuid(), "3101111111", "REC-1", 1000m, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        handler.Calls.Should().Be(1);
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
            if (receipt is not null) response.Headers.Add("X-Fiscal-Reference", receipt);
            return Task.FromResult(response);
        }
    }
}

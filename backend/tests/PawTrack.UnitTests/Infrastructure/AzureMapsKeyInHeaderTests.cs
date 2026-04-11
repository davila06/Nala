using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace PawTrack.UnitTests.Infrastructure;

/// <summary>
/// Round-10 security: Azure Maps services must pass the subscription key
/// as the "Ocp-Apim-Subscription-Key" HTTP header rather than as a
/// "subscription-key" URL query parameter.
///
/// Placing API keys in URL query strings causes them to appear in:
///   - Application Insights request telemetry (URL field)
///   - Reverse proxy / load balancer access logs
///   - Browser address-bar history (if ever called client-side)
///
/// The header approach keeps the secret out of log sinks.
/// </summary>
public sealed class AzureMapsKeyInHeaderTests
{
    private const string FakeKey      = "test-subscription-key-1234";
    private const string FakeEndpoint = "https://atlas.microsoft.com";

    // ── helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds an IConfiguration that contains only the Azure Maps key.
    /// </summary>
    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureMaps:SubscriptionKey"] = FakeKey,
                ["Azure:Vision:Endpoint"]     = FakeEndpoint,
                ["Azure:Vision:Key"]           = FakeKey,
            })
            .Build();

    /// <summary>
    /// Creates an HttpMessageHandler that captures the last request and
    /// returns the given JSON body with HTTP 200.
    /// </summary>
    private static (CapturingHandler handler, IHttpClientFactory factory) MakeFactory(
        string responseJson,
        string clientName = "AzureMaps")
    {
        var handler = new CapturingHandler(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
            });

        var client  = new HttpClient(handler) { BaseAddress = new Uri("https://atlas.microsoft.com") };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(clientName).Returns(client);

        return (handler, factory);
    }

    // ── AzureMapsGeocodingService — GeocodeAsync ──────────────────────────────

    [Fact]
    public async Task GeocodeAsync_DoesNotPutSubscriptionKeyInQueryString()
    {
        var geocodeJson = """
            { "results": [{ "score": 0.95, "position": { "lat": 9.9, "lon": -84.1 } }] }
            """;
        var (handler, factory) = MakeFactory(geocodeJson);
        var sut = new PawTrack.Infrastructure.Bot.AzureMapsGeocodingService(
            factory, BuildConfig(), NullLogger<PawTrack.Infrastructure.Bot.AzureMapsGeocodingService>.Instance);

        await sut.GeocodeAsync("San José, Costa Rica");

        handler.CapturedRequest.Should().NotBeNull();
        handler.CapturedRequest!.RequestUri!.Query
            .Should().NotContain("subscription-key",
                "the API key must not appear in the URL query string (it is logged there)");
    }

    [Fact]
    public async Task GeocodeAsync_SendsSubscriptionKeyAsHeader()
    {
        var geocodeJson = """
            { "results": [{ "score": 0.95, "position": { "lat": 9.9, "lon": -84.1 } }] }
            """;
        var (handler, factory) = MakeFactory(geocodeJson);
        var sut = new PawTrack.Infrastructure.Bot.AzureMapsGeocodingService(
            factory, BuildConfig(), NullLogger<PawTrack.Infrastructure.Bot.AzureMapsGeocodingService>.Instance);

        await sut.GeocodeAsync("San José, Costa Rica");

        handler.CapturedRequest.Should().NotBeNull();
        handler.CapturedRequest!.Headers
            .Should().ContainKey("Ocp-Apim-Subscription-Key",
                "the API key must be sent in the Ocp-Apim-Subscription-Key header");

        handler.CapturedRequest.Headers.GetValues("Ocp-Apim-Subscription-Key")
            .Should().ContainSingle(v => v == FakeKey);
    }

    // ── AzureMapsGeocodingService — ResolveCantonAsync ────────────────────────

    [Fact]
    public async Task ResolveCantonAsync_DoesNotPutSubscriptionKeyInQueryString()
    {
        var reverseJson = """
            { "addresses": [{ "address": { "municipality": "San José" } }] }
            """;
        var (handler, factory) = MakeFactory(reverseJson);
        var sut = new PawTrack.Infrastructure.Bot.AzureMapsGeocodingService(
            factory, BuildConfig(), NullLogger<PawTrack.Infrastructure.Bot.AzureMapsGeocodingService>.Instance);

        await sut.ResolveCantonAsync(9.9, -84.1);

        handler.CapturedRequest.Should().NotBeNull();
        handler.CapturedRequest!.RequestUri!.Query
            .Should().NotContain("subscription-key",
                "the API key must not appear in the reverse-geocoding URL query string");
    }

    [Fact]
    public async Task ResolveCantonAsync_SendsSubscriptionKeyAsHeader()
    {
        var reverseJson = """
            { "addresses": [{ "address": { "municipality": "San José" } }] }
            """;
        var (handler, factory) = MakeFactory(reverseJson);
        var sut = new PawTrack.Infrastructure.Bot.AzureMapsGeocodingService(
            factory, BuildConfig(), NullLogger<PawTrack.Infrastructure.Bot.AzureMapsGeocodingService>.Instance);

        await sut.ResolveCantonAsync(9.9, -84.1);

        handler.CapturedRequest!.Headers
            .Should().ContainKey("Ocp-Apim-Subscription-Key");
        handler.CapturedRequest.Headers.GetValues("Ocp-Apim-Subscription-Key")
            .Should().ContainSingle(v => v == FakeKey);
    }

    // ── AzureMapsIpGeoLookupService ───────────────────────────────────────────

    [Fact]
    public async Task IpGeoLookup_DoesNotPutSubscriptionKeyInQueryString()
    {
        var geoJson = """
            { "countryRegion": { "isoCode": "CR" }, "ipAddress": "1.2.3.4" }
            """;
        var (handler, factory) = MakeFactory(geoJson);

        var sut = BuildIpGeoService(factory);
        await sut.LookupAsync("1.2.3.4");

        handler.CapturedRequest.Should().NotBeNull();
        handler.CapturedRequest!.RequestUri!.Query
            .Should().NotContain("subscription-key",
                "the API key must not be in the IpGeoLookup URL query string");
    }

    [Fact]
    public async Task IpGeoLookup_SendsSubscriptionKeyAsHeader()
    {
        var geoJson = """
            { "countryRegion": { "isoCode": "CR" }, "ipAddress": "1.2.3.4" }
            """;
        var (handler, factory) = MakeFactory(geoJson);

        var sut = BuildIpGeoService(factory);
        await sut.LookupAsync("1.2.3.4");

        handler.CapturedRequest!.Headers
            .Should().ContainKey("Ocp-Apim-Subscription-Key");
        handler.CapturedRequest.Headers.GetValues("Ocp-Apim-Subscription-Key")
            .Should().ContainSingle(v => v == FakeKey);
    }

    /// <summary>
    /// Instantiates <c>AzureMapsIpGeoLookupService</c> (internal sealed) via reflection,
    /// creating the required <c>ILogger&lt;T&gt;</c> instance dynamically using
    /// <c>NullLogger&lt;T&gt;</c> so the concrete generic type resolves correctly.
    /// </summary>
    private static PawTrack.Application.Common.Interfaces.IIpGeoLookupService BuildIpGeoService(
        IHttpClientFactory factory)
    {
        var serviceType = typeof(PawTrack.Infrastructure.InfrastructureServiceCollectionExtensions)
            .Assembly
            .GetType("PawTrack.Infrastructure.Pets.AzureMapsIpGeoLookupService")!;

        // Build NullLogger<AzureMapsIpGeoLookupService> reflectively to satisfy
        // the constructor's ILogger<T> parameter without a direct type reference.
        // NullLogger<T>.Instance is a static readonly field, not a property.
        var nullLoggerType = typeof(NullLogger<>).MakeGenericType(serviceType);
        var logger = nullLoggerType.GetField("Instance",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!
            .GetValue(null)!;

        return (PawTrack.Application.Common.Interfaces.IIpGeoLookupService)
            Activator.CreateInstance(serviceType, factory, BuildConfig(), logger)!;
    }

    // ── helper types ──────────────────────────────────────────────────────────

    internal sealed class CapturingHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            return Task.FromResult(responder(request));
        }
    }
}

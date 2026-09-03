using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Infrastructure.Broadcast.Channels;

namespace PawTrack.UnitTests.Broadcast;

/// <summary>
/// Coverage for <see cref="FacebookChannelBroadcaster"/> — previously untested despite
/// making a real HTTP call to the Graph API (see docs/TODOs.md §13.2).
/// </summary>
public sealed class FacebookChannelBroadcasterTests
{
    private const string PageAccessToken = "test-page-access-token";
    private const string PageId = "123456789";

    private static IConfiguration BuildConfig(bool configured = true) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(configured
                ? new Dictionary<string, string?>
                {
                    ["Broadcast:Facebook:PageAccessToken"] = PageAccessToken,
                    ["Broadcast:Facebook:PageId"] = PageId,
                }
                : new Dictionary<string, string?>())
            .Build();

    private static BroadcastMessageContext MakeContext() =>
        new(
            LostPetEventId: Guid.NewGuid(),
            PetName: "Firulais",
            PetSpecies: "Dog",
            PetBreed: "Mestizo",
            OwnerEmail: "owner@example.com",
            OwnerContactPhone: null,
            OwnerContactName: "María",
            PetProfileUrl: "https://pawtrack.cr/p/firulais",
            TrackingUrl: "https://pawtrack.cr/t/abc123",
            RecentPhotoUrl: null,
            LastSeenAt: DateTimeOffset.UtcNow,
            LastSeenDescription: "Cerca del parque",
            RestrictToPaidChannels: false,
            NearbyFeaturedClinics: null);

    private static (RecordingHandler handler, IHttpClientFactory factory) MakeFactory(
        HttpStatusCode status = HttpStatusCode.OK, string? postId = "999888777")
    {
        var handler = new RecordingHandler(_ =>
        {
            var body = status == HttpStatusCode.OK
                ? $$"""{ "id": "{{postId}}" }"""
                : """{ "error": { "message": "Invalid OAuth access token." } }""";
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
            };
        });

        var client = new HttpClient(handler);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("Facebook").Returns(client);
        return (handler, factory);
    }

    [Fact]
    public void IsEnabled_WithoutCredentials_ReturnsFalse()
    {
        var sut = new FacebookChannelBroadcaster(
            Substitute.For<IHttpClientFactory>(), BuildConfig(configured: false), NullLogger<FacebookChannelBroadcaster>.Instance);

        sut.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void IsEnabled_WithCredentials_ReturnsTrue()
    {
        var sut = new FacebookChannelBroadcaster(
            Substitute.For<IHttpClientFactory>(), BuildConfig(), NullLogger<FacebookChannelBroadcaster>.Instance);

        sut.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_WithoutCredentials_ReturnsNullAndDoesNotCallHttpClient()
    {
        var (handler, factory) = MakeFactory();
        var sut = new FacebookChannelBroadcaster(factory, BuildConfig(configured: false), NullLogger<FacebookChannelBroadcaster>.Instance);

        var result = await sut.SendAsync(MakeContext());

        result.Should().BeNull();
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task SendAsync_WhenConfigured_PostsToGraphApiFeedEndpointWithAccessToken()
    {
        var (handler, factory) = MakeFactory();
        var sut = new FacebookChannelBroadcaster(factory, BuildConfig(), NullLogger<FacebookChannelBroadcaster>.Instance);

        await sut.SendAsync(MakeContext());

        handler.Requests.Should().ContainSingle();
        var request = handler.Requests[0];
        request.RequestUri!.ToString().Should().Contain($"/{PageId}/feed");

        var json = await request.Content!.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        doc.RootElement.GetProperty("access_token").GetString().Should().Be(PageAccessToken);
        doc.RootElement.GetProperty("message").GetString().Should().Contain("Firulais");
        doc.RootElement.GetProperty("link").GetString().Should().Be("https://pawtrack.cr/p/firulais");
    }

    [Fact]
    public async Task SendAsync_OnSuccess_ReturnsFacebookPostId()
    {
        var (_, factory) = MakeFactory(postId: "999888777");
        var sut = new FacebookChannelBroadcaster(factory, BuildConfig(), NullLogger<FacebookChannelBroadcaster>.Instance);

        var result = await sut.SendAsync(MakeContext());

        result.Should().Be("999888777");
    }

    [Fact]
    public async Task SendAsync_OnGraphApiError_ReturnsNull()
    {
        var (_, factory) = MakeFactory(status: HttpStatusCode.BadRequest);
        var sut = new FacebookChannelBroadcaster(factory, BuildConfig(), NullLogger<FacebookChannelBroadcaster>.Instance);

        var result = await sut.SendAsync(MakeContext());

        result.Should().BeNull();
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(responder(request));
        }
    }
}

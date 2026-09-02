using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Infrastructure.Broadcast.Channels;

namespace PawTrack.UnitTests.Broadcast;

/// <summary>
/// Regression coverage for the "Logo en alertas de pérdida" ClinicPlus bug:
/// <see cref="NearbyClinicRef"/> previously had no LogoUrl, so featured clinics
/// were mentioned only as text on WhatsApp, never as a visible logo image.
/// WhatsApp text messages cannot embed inline images, so the fix sends the
/// sponsoring clinic's logo as a separate "image" message.
/// </summary>
public sealed class WhatsAppChannelBroadcasterTests
{
    private const string PhoneNumberId = "1234567890";
    private const string AccessToken = "test-token";
    private const string RecipientListUrl = "+50688887777";

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Broadcast:WhatsApp:PhoneNumberId"] = PhoneNumberId,
                ["Broadcast:WhatsApp:AccessToken"] = AccessToken,
                ["Broadcast:WhatsApp:RecipientListUrl"] = RecipientListUrl,
            })
            .Build();

    private static BroadcastMessageContext MakeContext(IReadOnlyList<NearbyClinicRef>? nearbyClinics) =>
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
            NearbyFeaturedClinics: nearbyClinics);

    private static (RecordingHandler handler, IHttpClientFactory factory) MakeFactory()
    {
        var handler = new RecordingHandler(req =>
        {
            var json = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? "{}";
            var isImage = json.Contains("\"type\":\"image\"");
            var body = isImage
                ? """{ "messages": [{ "id": "wamid.IMAGE" }] }"""
                : """{ "messages": [{ "id": "wamid.TEXT" }] }""";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
            };
        });

        var client = new HttpClient(handler);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("MetaWhatsApp").Returns(client);
        return (handler, factory);
    }

    [Fact]
    public async Task SendAsync_WithFeaturedClinicLogo_SendsSeparateImageMessage()
    {
        var clinic = new NearbyClinicRef("Clínica San Rafael", "+50622223333", "San José", "https://cdn.pawtrack.cr/logos/sanrafael.png");
        var (handler, factory) = MakeFactory();
        var sut = new WhatsAppChannelBroadcaster(factory, BuildConfig(), NullLogger<WhatsAppChannelBroadcaster>.Instance);

        await sut.SendAsync(MakeContext([clinic]));

        handler.Requests.Should().HaveCount(2, "one text message plus one image message for the sponsor logo");

        var imageRequestJson = await handler.Requests[1].Content!.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(imageRequestJson);
        var root = doc.RootElement;
        root.GetProperty("type").GetString().Should().Be("image");
        var image = root.GetProperty("image");
        image.GetProperty("link").GetString().Should().Be(clinic.LogoUrl,
            "the clinic's actual logo URL must be delivered, not just its name in text");
        image.GetProperty("caption").GetString().Should().Contain(clinic.Name);
    }

    [Fact]
    public async Task SendAsync_WithoutFeaturedClinics_SendsOnlyTextMessage()
    {
        var (handler, factory) = MakeFactory();
        var sut = new WhatsAppChannelBroadcaster(factory, BuildConfig(), NullLogger<WhatsAppChannelBroadcaster>.Instance);

        await sut.SendAsync(MakeContext(null));

        handler.Requests.Should().ContainSingle();
        var json = await handler.Requests[0].Content!.ReadAsStringAsync();
        json.Should().Contain("\"type\":\"text\"");
    }

    [Fact]
    public async Task SendAsync_WithClinicMissingLogoUrl_DoesNotSendImageMessage()
    {
        var clinic = new NearbyClinicRef("Clínica Sin Logo", null, "Heredia", LogoUrl: null);
        var (handler, factory) = MakeFactory();
        var sut = new WhatsAppChannelBroadcaster(factory, BuildConfig(), NullLogger<WhatsAppChannelBroadcaster>.Instance);

        await sut.SendAsync(MakeContext([clinic]));

        handler.Requests.Should().ContainSingle("no logo means no image message can be sent");
    }

    [Fact]
    public async Task SendAsync_SponsorImageFailure_DoesNotThrowOrBlockMainMessage()
    {
        var clinic = new NearbyClinicRef("Clínica Falla", null, "Cartago", "https://cdn.pawtrack.cr/logos/broken.png");
        var handler = new RecordingHandler(req =>
        {
            var json = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? "{}";
            if (json.Contains("\"type\":\"image\""))
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("boom"),
                };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{ "messages": [{ "id": "wamid.TEXT" }] }""",
                    System.Text.Encoding.UTF8, "application/json"),
            };
        });
        var client = new HttpClient(handler);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("MetaWhatsApp").Returns(client);
        var sut = new WhatsAppChannelBroadcaster(factory, BuildConfig(), NullLogger<WhatsAppChannelBroadcaster>.Instance);

        var act = async () => await sut.SendAsync(MakeContext([clinic]));

        await act.Should().NotThrowAsync();
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

using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Infrastructure.Broadcast.Channels;

namespace PawTrack.UnitTests.Broadcast;

/// <summary>
/// Regression coverage for the Telegram side of the "Logo en alertas de pérdida" bug.
/// Prior to this fix, <c>TelegramChannelBroadcaster.BuildMessage</c> never referenced
/// <see cref="BroadcastMessageContext.NearbyFeaturedClinics"/> at all — clinics were not
/// mentioned in Telegram alerts, not even as text. The fix adds a text mention plus a
/// dedicated sendPhoto call for the sponsoring clinic's logo.
/// </summary>
public sealed class TelegramChannelBroadcasterTests
{
    private const string BotToken = "test-bot-token";
    private const string ChatId = "-1001234567890";

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Broadcast:Telegram:BotToken"] = BotToken,
                ["Broadcast:Telegram:ChatId"] = ChatId,
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
            var isPhoto = req.RequestUri!.ToString().Contains("sendPhoto");
            var body = isPhoto
                ? """{ "ok": true, "result": { "message_id": 999 } }"""
                : """{ "ok": true, "result": { "message_id": 42 } }""";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
            };
        });

        var client = new HttpClient(handler);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("Telegram").Returns(client);
        return (handler, factory);
    }

    [Fact]
    public async Task SendAsync_WithFeaturedClinics_MentionsClinicNameInMessageText()
    {
        var clinic = new NearbyClinicRef("Clínica San Rafael", "+50622223333", "San José", LogoUrl: null);
        var (handler, factory) = MakeFactory();
        var sut = new TelegramChannelBroadcaster(factory, BuildConfig(), NullLogger<TelegramChannelBroadcaster>.Instance);

        await sut.SendAsync(MakeContext([clinic]));

        var sendMessageRequest = handler.Requests.Single(r => r.RequestUri!.ToString().Contains("sendMessage"));
        var json = await sendMessageRequest.Content!.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var text = doc.RootElement.GetProperty("text").GetString();
        text.Should().Contain(clinic.Name, "clinics must be mentioned in the Telegram message text, not silently dropped");
    }

    [Fact]
    public async Task SendAsync_WithFeaturedClinicLogo_SendsSeparateSendPhotoCall()
    {
        var clinic = new NearbyClinicRef("Clínica San Rafael", "+50622223333", "San José", "https://cdn.pawtrack.cr/logos/sanrafael.png");
        var (handler, factory) = MakeFactory();
        var sut = new TelegramChannelBroadcaster(factory, BuildConfig(), NullLogger<TelegramChannelBroadcaster>.Instance);

        await sut.SendAsync(MakeContext([clinic]));

        handler.Requests.Should().HaveCount(2, "one sendMessage plus one sendPhoto for the sponsor logo");
        var photoRequest = handler.Requests.Single(r => r.RequestUri!.ToString().Contains("sendPhoto"));
        var json = await photoRequest.Content!.ReadAsStringAsync();
        json.Should().Contain(clinic.LogoUrl!,
            "the clinic's actual logo URL must be delivered via sendPhoto, not just mentioned as text");
    }

    [Fact]
    public async Task SendAsync_WithoutFeaturedClinics_DoesNotCallSendPhoto()
    {
        var (handler, factory) = MakeFactory();
        var sut = new TelegramChannelBroadcaster(factory, BuildConfig(), NullLogger<TelegramChannelBroadcaster>.Instance);

        await sut.SendAsync(MakeContext(null));

        handler.Requests.Should().ContainSingle();
        handler.Requests[0].RequestUri!.ToString().Should().Contain("sendMessage");
    }

    [Fact]
    public async Task SendAsync_WithClinicMissingLogoUrl_DoesNotCallSendPhoto()
    {
        var clinic = new NearbyClinicRef("Clínica Sin Logo", null, "Heredia", LogoUrl: null);
        var (handler, factory) = MakeFactory();
        var sut = new TelegramChannelBroadcaster(factory, BuildConfig(), NullLogger<TelegramChannelBroadcaster>.Instance);

        await sut.SendAsync(MakeContext([clinic]));

        handler.Requests.Should().ContainSingle("no logo means sendPhoto must not be called");
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

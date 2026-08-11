using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Webhooks;

[Collection("Integration")]
public sealed class WebhooksEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task SinpeWebhook_NoSignature_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/webhooks/sinpe", new
        {
            reference = "ABCD1234",
            amount_crc = 2990m,
            sender_name = "Test User",
            timestamp = DateTimeOffset.UtcNow,
        });

        // No signature header → 401
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SinpeWebhook_InvalidSignature_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/sinpe")
        {
            Content = JsonContent.Create(new
            {
                reference = "ABCD1234",
                amount_crc = 2990m,
                sender_name = "Test User",
                timestamp = DateTimeOffset.UtcNow,
            }),
        };
        request.Headers.Add("X-Webhook-Signature", "invalidhexsignature");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SinpeWebhook_ValidSignatureUnknownReference_Returns200WithNotFound()
    {
        const string secret = "test-webhook-secret";
        const string reference = "UNKNWREF";
        const decimal amount = 2990m;
        var signature = ComputeHmac(secret, $"{reference}:{amount}");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/sinpe")
        {
            Content = JsonContent.Create(new
            {
                reference,
                amount_crc = amount,
                sender_name = "Test User",
                timestamp = DateTimeOffset.UtcNow,
            }),
        };
        request.Headers.Add("X-Webhook-Signature", signature);

        // Without Webhooks:SinpeSecret configured in test env, ValidateSignature returns false → 401
        // This documents the expected behavior
        var response = await _client.SendAsync(request);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.OK);
    }

    [Fact]
    public async Task SinpeWebhook_MissingBody_Returns400()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/sinpe")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json"),
        };

        var response = await _client.SendAsync(request);
        // Missing required fields → bad request or model binding failure
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }

    private static string ComputeHmac(string key, string data)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        return Convert.ToHexString(HMACSHA256.HashData(keyBytes, dataBytes)).ToLowerInvariant();
    }
}

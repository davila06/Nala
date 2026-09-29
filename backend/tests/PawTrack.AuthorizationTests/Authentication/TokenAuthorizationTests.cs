using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using PawTrack.AuthorizationTests.Infrastructure;

namespace PawTrack.AuthorizationTests.Authentication;

[Collection("Authorization")]
public sealed class TokenAuthorizationTests
{
    private const string SigningKey = "authorization-tests-only-key-minimum-256-bits-for-hmac";
    private readonly AuthorizationTestFactory factory;

    public TokenAuthorizationTests(AuthorizationTestFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task AdminAuditEndpoint_RejectsMalformedBearerToken()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "not-a-jwt");

        using var response = await client.GetAsync("/api/admin/audit");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminAuditEndpoint_RejectsExpiredToken()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", CreateToken("Admin", DateTimeOffset.UtcNow.AddMinutes(-5)));

        using var response = await client.GetAsync("/api/admin/audit");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminAuditEndpoint_RejectsAuthenticatedUserWithWrongRole()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", CreateToken("Owner", DateTimeOffset.UtcNow.AddMinutes(5)));

        using var response = await client.GetAsync("/api/admin/audit");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
    }

    private static string CreateToken(string role, DateTimeOffset expiresAt)
    {
        var header = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new { alg = "HS256", typ = "JWT" }));
        var payload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new
        {
            sub = "00000000-0000-0000-0000-000000000001",
            role,
            sid = "authorization-test-session",
            iss = "pawtrack-authorization-tests",
            aud = "pawtrack-authorization-tests",
            iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            exp = expiresAt.ToUnixTimeSeconds(),
            jti = Guid.NewGuid().ToString("N"),
        }));
        var unsignedToken = $"{header}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SigningKey));
        var signature = Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(unsignedToken)));
        return $"{unsignedToken}.{signature}";
    }

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

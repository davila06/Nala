using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Auth;

public sealed class AuthEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_ValidPayload_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Denis Avila",
            email = $"test_{Guid.NewGuid()}@pawtrack.cr",
            password = "SecurePass1!",
        });

        // Anti-enumeration: response body must NOT expose userId or reveal
        // whether the email was new — only a neutral 201 + message.
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_MissingFields_Returns422()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "",
            email = "invalid",
            password = "short",
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns201_WithoutRevealingExistence()
    {
        // Anti-enumeration: a duplicate registration returns the same 201 as a
        // successful one so the caller cannot enumerate account existence.
        var email = $"dup_{Guid.NewGuid()}@pawtrack.cr";

        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "User One",
            email,
            password = "SecurePass1!",
        });

        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "User Two",
            email,
            password = "SecurePass1!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Login_UnverifiedEmail_Returns401()
    {
        var email = $"unverified_{Guid.NewGuid()}@pawtrack.cr";

        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Unverified User",
            email,
            password = "SecurePass1!",
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "SecurePass1!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_NoCookie_Returns401()
    {
        var response = await _client.PostAsync("/api/auth/refresh", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_Returns202()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", new
        {
            email = $"ghost_{Guid.NewGuid()}@pawtrack.cr",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", new
        {
            token = "invalid-token",
            newPassword = "SecurePass1!",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record RegisterResponse(string Message);
}

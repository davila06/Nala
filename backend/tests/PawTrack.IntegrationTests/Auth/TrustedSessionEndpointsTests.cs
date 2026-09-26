using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.Application.Common.Interfaces;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Auth;

[Collection("Integration")]
public sealed class TrustedSessionEndpointsTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    [Fact]
    public async Task UserCanListAndRevokeOnlyTheirOwnSessions()
    {
        var email = $"session-owner-{Guid.NewGuid():N}@pawtrack.cr";
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory, email);
        var outsiderEmail = $"session-outsider-{Guid.NewGuid():N}@pawtrack.cr";
        var outsider = await AuthHelper.CreateAuthenticatedClientAsync(factory, outsiderEmail);

        var response = await client.GetAsync("/api/auth/me/sessions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var sessions = await response.Content.ReadFromJsonAsync<List<SessionResponse>>();
        sessions.Should().NotBeNullOrEmpty();
        var session = sessions!.Should().ContainSingle(item => item.IsCurrent).Subject;
        var outsiderMfaToken = await GetMfaSessionTokenAsync(outsiderEmail);
        outsider.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", outsiderMfaToken);
        var outsiderSessions = await outsider.GetFromJsonAsync<List<SessionResponse>>("/api/auth/me/sessions");
        outsiderSessions.Should().NotContain(item => item.SessionId == session.SessionId);
        var crossUserRevoke = await outsider.DeleteAsync($"/api/auth/me/sessions/{session.SessionId}");
        crossUserRevoke.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var ownToken = await GetMfaSessionTokenAsync(email);
        string siblingAccessToken;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var user = await db.Users.SingleAsync(candidate => candidate.Email == email);
            siblingAccessToken = jwt.GenerateAccessToken(user.Id, user.Email, user.Name, user.Role,
                mfaVerified: true, sessionId: session.SessionId);
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownToken);
        var revoked = await client.DeleteAsync($"/api/auth/me/sessions/{session.SessionId}");
        revoked.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
        var userId = await verifyDb.Users.Where(user => user.Email == email).Select(user => user.Id).SingleAsync();
        (await verifyDb.RefreshTokens.AnyAsync(token => token.UserId == userId && !token.IsRevoked)).Should().BeFalse();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", siblingAccessToken);
        var siblingAccess = await client.GetAsync("/api/auth/me");
        siblingAccess.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TrustedDeviceEnrollmentRequiresMfaAndCanBeRevoked()
    {
        var email = $"trusted-device-{Guid.NewGuid():N}@pawtrack.cr";
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory, email);
        var outsiderEmail = $"trusted-outsider-{Guid.NewGuid():N}@pawtrack.cr";
        var outsider = await AuthHelper.CreateAuthenticatedClientAsync(factory, outsiderEmail);

        var denied = await client.PostAsJsonAsync("/api/auth/me/trusted-devices", new { deviceName = "Recepción" });
        denied.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var token = await GetMfaSessionTokenAsync(email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var enrolled = await client.PostAsJsonAsync("/api/auth/me/trusted-devices", new { deviceName = "Recepción" });
        enrolled.StatusCode.Should().Be(HttpStatusCode.Created);
        var device = await enrolled.Content.ReadFromJsonAsync<TrustedDeviceResponse>();
        device.Should().NotBeNull();
        var trustedCookie = ReadCookie(enrolled, "trustedDevice");
        trustedCookie.Should().NotBeNullOrWhiteSpace();

        var devices = await client.GetFromJsonAsync<List<TrustedDeviceResponse>>("/api/auth/me/trusted-devices");
        devices.Should().ContainSingle(item => item.Id == device!.Id && item.DeviceName == "Recepción");

        var outsiderToken = await GetMfaSessionTokenAsync(outsiderEmail);
        outsider.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", outsiderToken);
        var foreignRevoke = await outsider.DeleteAsync($"/api/auth/me/trusted-devices/{device.Id}");
        foreignRevoke.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var revoked = await client.DeleteAsync($"/api/auth/me/trusted-devices/{device!.Id}");
        revoked.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetFromJsonAsync<List<TrustedDeviceResponse>>("/api/auth/me/trusted-devices"))
            .Should().NotContain(item => item.Id == device.Id);

        using var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email, password = "SecurePass1!" }),
        };
        loginRequest.Headers.Add("Cookie", $"trustedDevice={trustedCookie}");
        var login = await client.SendAsync(loginRequest);
        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TrustedDeviceLoginRotatesProofAndRefreshDoesNotPreserveMfaElevation()
    {
        var email = $"trusted-login-{Guid.NewGuid():N}@pawtrack.cr";
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory, email);
        client.DefaultRequestHeaders.Authorization = null;
        var mfaToken = await GetMfaSessionTokenAsync(email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mfaToken);

        var enrollment = await client.PostAsJsonAsync("/api/auth/me/trusted-devices", new { deviceName = "Laptop clínica" });
        enrollment.StatusCode.Should().Be(HttpStatusCode.Created);
        var trustedCookie = ReadCookie(enrollment, "trustedDevice");
        trustedCookie.Should().NotBeNullOrWhiteSpace();

        client.DefaultRequestHeaders.Authorization = null;
        using var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email, password = "SecurePass1!" }),
        };
        loginRequest.Headers.Add("Cookie", $"trustedDevice={trustedCookie}");
        var login = await client.SendAsync(loginRequest);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginBody = await login.Content.ReadFromJsonAsync<JsonElement>();
        loginBody.TryGetProperty("renewedTrustedDeviceToken", out _).Should().BeFalse();
        var loginAccessToken = loginBody.GetProperty("accessToken").GetString()!;
        var loginJwt = new JwtSecurityTokenHandler().ReadJwtToken(loginAccessToken);
        loginJwt.Claims.Should().Contain(claim => claim.Type == "mfa" && claim.Value == "true");
        var sessionId = loginJwt.Claims.Single(claim => claim.Type == "sid").Value;

        var rotatedTrustedCookie = ReadCookie(login, "trustedDevice");
        rotatedTrustedCookie.Should().NotBeNullOrWhiteSpace().And.NotBe(trustedCookie);
        var refreshCookie = ReadCookie(login, "refreshToken");
        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshRequest.Headers.Add("Cookie", $"refreshToken={refreshCookie}");
        var refresh = await client.SendAsync(refreshRequest);
        refresh.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshedBody = await refresh.Content.ReadFromJsonAsync<JsonElement>();
        var refreshedJwt = new JwtSecurityTokenHandler().ReadJwtToken(refreshedBody.GetProperty("accessToken").GetString()!);
        refreshedJwt.Claims.Should().NotContain(claim => claim.Type == "mfa");
        refreshedJwt.Claims.Single(claim => claim.Type == "sid").Value.Should().Be(sessionId);
    }

    [Fact]
    public async Task TrustedDeviceCannotReplaceMfaForAdminLogin()
    {
        var email = $"trusted-admin-{Guid.NewGuid():N}@pawtrack.cr";
        var client = await AuthHelper.CreateAdminClientAsync(factory, email);
        var mfaToken = await GetMfaSessionTokenAsync(email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mfaToken);
        var enrolled = await client.PostAsJsonAsync("/api/auth/me/trusted-devices", new { deviceName = "Admin laptop" });
        enrolled.StatusCode.Should().Be(HttpStatusCode.Created);
        var trustedCookie = ReadCookie(enrolled, "trustedDevice");

        using var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email, password = "SecurePass1!" }),
        };
        loginRequest.Headers.Add("Cookie", $"trustedDevice={trustedCookie}");
        var login = await client.SendAsync(loginRequest);
        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MfaStepUpIssuesElevatedTokenForSameSessionAndConsumesRecoveryCode()
    {
        var email = $"mfa-stepup-{Guid.NewGuid():N}@pawtrack.cr";
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory, email);
        string recoveryCode;
        Guid sessionId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var user = await db.Users.SingleAsync(candidate => candidate.Email == email);
            recoveryCode = user.ConfigureMfa("integration-protected-secret")[0];
            await db.SaveChangesAsync();
            sessionId = await db.RefreshTokens
                .Where(token => token.UserId == user.Id && !token.IsRevoked)
                .OrderByDescending(token => token.CreatedAt)
                .Select(token => token.SessionId)
                .FirstAsync();
        }

        var stepUp = await client.PostAsJsonAsync("/api/auth/mfa/step-up", new { code = recoveryCode });
        stepUp.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await stepUp.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = new JwtSecurityTokenHandler().ReadJwtToken(payload.GetProperty("accessToken").GetString()!);
        accessToken.Claims.Should().Contain(claim => claim.Type == "mfa" && claim.Value == "true");
        accessToken.Claims.Single(claim => claim.Type == "sid").Value.Should().Be(sessionId.ToString("N"));

        var replay = await client.PostAsJsonAsync("/api/auth/mfa/step-up", new { code = recoveryCode });
        replay.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task DisablingMfaRevokesAllSessionsAndTrustedDevices()
    {
        var email = $"mfa-disable-{Guid.NewGuid():N}@pawtrack.cr";
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory, email);
        var elevatedToken = await GetMfaSessionTokenAsync(email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", elevatedToken);

        var disabled = await client.DeleteAsync("/api/auth/mfa");
        disabled.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var userId = await db.Users.Where(user => user.Email == email).Select(user => user.Id).SingleAsync();
            (await db.RefreshTokens.AnyAsync(token => token.UserId == userId && !token.IsRevoked)).Should().BeFalse();
            (await db.TrustedDevices.AnyAsync(device => device.UserId == userId && device.RevokedAt == null)).Should().BeFalse();
        }

        var staleAccess = await client.GetAsync("/api/auth/me");
        staleAccess.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<string> GetMfaSessionTokenAsync(string email)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var user = await db.Users.SingleAsync(candidate => candidate.Email == email);
        user.ConfigureMfa("test-protected-secret");
        await db.SaveChangesAsync();
        var sessionId = await db.RefreshTokens
            .Where(token => token.UserId == user.Id && !token.IsRevoked)
            .OrderByDescending(token => token.CreatedAt)
            .Select(token => token.SessionId)
            .FirstAsync();
        return jwt.GenerateAccessToken(user.Id, user.Email, user.Name, user.Role,
            mfaVerified: true, sessionId: sessionId);
    }

    private static string? ReadCookie(HttpResponseMessage response, string name)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values)) return null;
        var cookie = values.FirstOrDefault(value => value.StartsWith($"{name}=", StringComparison.Ordinal));
        if (cookie is null) return null;
        var end = cookie.IndexOf(';');
        return cookie[(name.Length + 1)..(end < 0 ? cookie.Length : end)];
    }

    private sealed record SessionResponse(Guid SessionId, bool IsCurrent);
    private sealed record TrustedDeviceResponse(Guid Id, string DeviceName);
}

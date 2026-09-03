using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using PawTrack.Application.Auth.Commands.Register;
using PawTrack.Application.Auth.Commands.VerifyEmail;
using PawTrack.Application.Common.Interfaces;
using PawTrack.IntegrationTests.Infrastructure;

namespace PawTrack.IntegrationTests.Auth;

public sealed class AuthHelperDiagnosticTests(PawTrackWebApplicationFactory factory)
    : IClassFixture<PawTrackWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_EmailSenderStubCapturesToken()
    {
        var email = $"diag_{Guid.NewGuid():N}@test.cr";

        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Diag User",
            email,
            password = "SecurePass1!",
            isAdultConfirmed = true,
        });

        var token = factory.LastVerificationToken;
        token.Should().NotBeNullOrEmpty("CapturingEmailSender should set LastVerificationToken during registration");
    }

    [Fact]
    public async Task VerifyEmail_PersistsToInMemoryDB()
    {
        var email = $"persist_{Guid.NewGuid():N}@test.cr";

        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>() as CapturingEmailSender;

            await mediator.Send(new RegisterCommand("Test", email, "SecurePass1!", true));
            var token = emailSender!.LastVerificationToken!;

            var result = await mediator.Send(new VerifyEmailCommand(token));
            result.IsSuccess.Should().BeTrue("VerifyEmail should succeed");
        }

        // Read from InMemory in a NEW scope to verify persistence
        using (var scope2 = factory.Services.CreateScope())
        {
            var db = scope2.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            user.Should().NotBeNull();
            user!.IsEmailVerified.Should().BeTrue("InMemory DB should persist IsEmailVerified=true");
        }
    }

    [Fact]
    public void FactoryUsesTestJwtKey()
    {
        using var scope = factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        var jwtKey = config["Jwt:Key"];
        jwtKey.Should().Be("integration-tests-only-xK9#mP2$vQ8!nL5@wR3&jY7*",
            "factory should inject test JWT key");
    }

    [Fact]
    public async Task ManualJwt_GetMyProfile_Returns200()
    {
        // Create user + get their ID
        var email = $"manual_{Guid.NewGuid():N}@test.cr";
        Guid userId;

        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>() as CapturingEmailSender;
            await mediator.Send(new RegisterCommand("JWT Test", email, "SecurePass1!", true));
            var token = emailSender!.LastVerificationToken!;
            await mediator.Send(new VerifyEmailCommand(token));

            var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            userId = user!.Id;
        }

        // Manually generate JWT using the test key
        var config = factory.Services.GetRequiredService<IConfiguration>();
        var jwtKey = config["Jwt:Key"]!;
        var issuer = config["Jwt:Issuer"]!;
        var audience = config["Jwt:Audience"]!;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Name, "JWT Test"),
            new Claim(ClaimTypes.Role, "Owner"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        var jwt = new JwtSecurityToken(issuer, audience, claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);
        var tokenStr = new JwtSecurityTokenHandler().WriteToken(jwt);

        // Send request with manually generated token
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenStr);
        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "manually generated JWT with correct key/issuer/audience should be accepted");
    }

    [Fact]
    public async Task CreateAuthenticatedClient_ReturnsAuthenticatedClient()
    {
        var client = await AuthHelper.CreateAuthenticatedClientAsync(factory);

        var hasAuth = client.DefaultRequestHeaders.Authorization is not null;
        hasAuth.Should().BeTrue("login should have set Authorization header");

        var response = await client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK, "authenticated client should access protected endpoint");
    }
}

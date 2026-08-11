using System.Net.Http.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.Application.Auth.Commands.Register;
using PawTrack.Application.Auth.Commands.VerifyEmail;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.IntegrationTests.Infrastructure;

/// <summary>Creates a fully-verified, authenticated HttpClient for integration tests.</summary>
public static class AuthHelper
{
    private const string Password = "SecurePass1!";

    public static async Task<HttpClient> CreateAuthenticatedClientAsync(
        PawTrackWebApplicationFactory factory,
        string? email = null)
    {
        email = (email ?? $"test_{Guid.NewGuid():N}@pawtrack.cr").ToLowerInvariant();

        // Use MediatR directly — avoids HTTP round-trip and bypasses email verification cleanly
        using (var scope = factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

            // 1. Register via command — CapturingEmailSender captures the raw token
            var registerResult = await mediator.Send(new RegisterCommand("Test User", email, Password));

            // 2. Get the captured raw token from the singleton email sender
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>()
                as CapturingEmailSender;
            var token = emailSender?.LastVerificationToken
                ?? throw new InvalidOperationException($"CapturingEmailSender did not capture token for {email}. Register result: {registerResult}");

            // 3. Verify the email
            var verifyResult = await mediator.Send(new VerifyEmailCommand(token));
            if (verifyResult.IsFailure)
                throw new InvalidOperationException($"VerifyEmail failed: {string.Join(", ", verifyResult.Errors)}");
        }

        // 3. Login via HTTP to get JWT
        var client = factory.CreateClient();
        var loginResp = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        if (!loginResp.IsSuccessStatusCode) return client;

        var body = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();
        if (body?.AccessToken is not null)
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", body.AccessToken);

        return client;
    }

    /// <summary>Legacy overload for existing tests that pass an HttpClient directly.</summary>
    public static Task<(HttpClient Client, string Token)> RegisterAndLoginAsync(
        HttpClient client, string? email = null, string role = "User") =>
        Task.FromResult((client, string.Empty));

    private static void SetPrivate(object obj, string propertyName, object? value)
    {
        var setter = obj.GetType()
            .GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            ?.GetSetMethod(nonPublic: true);
        setter?.Invoke(obj, [value]);
    }

    /// <summary>Creates a verified, authenticated client with an active UserPlus subscription.</summary>
    public static async Task<HttpClient> CreatePlusClientAsync(
        PawTrackWebApplicationFactory factory,
        string? email = null)
    {
        var client = await CreateAuthenticatedClientAsync(factory, email);

        // Grant a Plus subscription directly via DbContext
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var emailStr = email ?? ExtractEmail(client);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == emailStr);
        if (user is not null)
        {
            var sub = Subscription.CreateForUser(
                user.Id, SubscriptionTier.UserPlus, "TEST-REF", 2990m);
            sub.Activate();
            await db.Subscriptions.AddAsync(sub);
            await db.SaveChangesAsync();
        }

        return client;
    }

    /// <summary>Creates a verified, authenticated client with the Municipality role.</summary>
    public static async Task<HttpClient> CreateMunicipalityClientAsync(
        PawTrackWebApplicationFactory factory,
        string? email = null)
    {
        email = (email ?? $"muni_{Guid.NewGuid():N}@pawtrack.cr").ToLowerInvariant();
        var client = await CreateAuthenticatedClientAsync(factory, email);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PawTrack.Infrastructure.Persistence.PawTrackDbContext>();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is not null)
        {
            user.AssignMunicipalityRole();
            db.Users.Update(user);
            await db.SaveChangesAsync();
        }

        // Re-login to get a JWT that includes the Municipality role claim
        var loginResp = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        if (loginResp.IsSuccessStatusCode)
        {
            var body = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();
            if (body?.AccessToken is not null)
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", body.AccessToken);
        }

        return client;
    }

    private static string ExtractEmail(HttpClient client)
    {
        var token = client.DefaultRequestHeaders.Authorization?.Parameter ?? "";
        if (string.IsNullOrEmpty(token)) return string.Empty;
        try
        {
            var payload = token.Split('.')[1];
            var padded = payload + new string('=', (4 - payload.Length % 4) % 4);
            var bytes = Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/'));
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("email", out var e) ? e.GetString() ?? "" : "";
        }
        catch { return string.Empty; }
    }

    private sealed record LoginResponse(string AccessToken, string RefreshToken);
}

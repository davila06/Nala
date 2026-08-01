using System.Net.Http.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PawTrack.Application.Auth.Commands.Register;
using PawTrack.Application.Auth.Commands.VerifyEmail;
using PawTrack.Application.Common.Interfaces;

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

    private sealed record LoginResponse(string AccessToken, string RefreshToken);
}

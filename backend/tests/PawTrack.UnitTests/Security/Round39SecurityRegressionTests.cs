using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using PawTrack.API.Controllers;
using System.Reflection;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-39 security regression tests.
///
/// Gap: <c>AuthController</c> has two high-value <c>[FromBody]</c> endpoints
/// that carry NO <c>[RequestSizeLimit]</c>:
///
/// ── <c>POST /api/auth/register</c> — <c>Register</c> ─────────────────────────
///   Body: <c>RegisterRequest(Name, Email, Password)</c>.
///   Realistic maximum: name ≤ 200 chars + email ≤ 254 chars + password ≤ 128 chars
///   ≈ 600 bytes at most. Without a per-action ceiling the endpoint accepts
///   payloads up to the global Kestrel limit (1 MB), triggering:
///     • full JSON deserialization of a megabyte blob
///     • FluentValidation pipeline (name/email/password validators)
///     • BCrypt hash computation (expensive CPU work in LoginCommand)
///   This makes the registration endpoint a low-effort CPU amplification vector.
///
/// ── <c>POST /api/auth/login</c> — <c>Login</c> ───────────────────────────────
///   Body: <c>LoginRequest(Email, Password)</c>.
///   Realistic maximum: email ≤ 254 + password ≤ 128 ≈ 400 bytes.
///   Without a ceiling, a 1 MB login request forces the ASP.NET runtime to buffer
///   the full body before the rate-limiter check fires. Because the "login"
///   policy allows 5 requests/min per IP, an attacker cycling 5 × 1 MB = 5 MB
///   of body data per minute per IP can sustain meaningful memory pressure at scale.
///
/// Fix:
///   <c>[RequestSizeLimit(1024)]</c> on <c>Register</c>
///   <c>[RequestSizeLimit(512)]</c>  on <c>Login</c>
/// </summary>
public sealed class Round39SecurityRegressionTests
{
    // ── Register ──────────────────────────────────────────────────────────────

    [Fact]
    public void AuthController_Register_HasRequestSizeLimitAttribute()
    {
        // Arrange
        var method = typeof(AuthController)
            .GetMethod("Register", BindingFlags.Public | BindingFlags.Instance);

        // Act
        var attr = method?.GetCustomAttribute<RequestSizeLimitAttribute>();

        // Assert
        attr.Should().NotBeNull(
            "POST /api/auth/register accepts [FromBody] with name+email+password; " +
            "a per-action [RequestSizeLimit] prevents 1 MB JSON blobs from triggering " +
            "full deserialization + BCrypt hashing before rate-limiting applies.");
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Fact]
    public void AuthController_Login_HasRequestSizeLimitAttribute()
    {
        // Arrange
        var method = typeof(AuthController)
            .GetMethod("Login", BindingFlags.Public | BindingFlags.Instance);

        // Act
        var attr = method?.GetCustomAttribute<RequestSizeLimitAttribute>();

        // Assert
        attr.Should().NotBeNull(
            "POST /api/auth/login accepts [FromBody] with email+password; " +
            "without a per-action ceiling an attacker can send 1 MB bodies — " +
            "the BCrypt comparator is invoked after deserialization, making " +
            "this a CPU amplification vector even at 5 req/min per IP.");
    }
}

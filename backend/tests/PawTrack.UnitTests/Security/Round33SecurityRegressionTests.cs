using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.API.Controllers;
using System.Reflection;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-33 security regression tests.
///
/// Gap: Three endpoints on <c>AuthController</c> carry <c>[Authorize]</c> but
/// have <b>zero <c>[EnableRateLimiting]</c></b>:
///
///   • <c>POST /api/auth/logout</c>   — <c>Logout</c>
///   • <c>GET  /api/auth/me</c>       — <c>GetMyProfile</c>
///   • <c>PATCH /api/auth/me</c>      — <c>UpdateMyProfile</c>
///
/// ── Attack 1: JTI blocklist flooding (Logout) ─────────────────────────────────
///   <c>LogoutCommand</c> performs the following operations on every call:
///     ① Revokes the refresh token in the DB (<c>UPDATE</c>)
///     ② Writes the current access token JTI to the revocation store
///        (<c>IJtiBlocklist.AddAsync</c> — backed by Redis)
///
///   Without rate limiting, a valid JWT (15-min window) can flood the Redis
///   blocklist / DB revocation table at full network speed before expiry.
///   This matters because:
///     a) Redis blocklist storage may be bounded — flooding it evicts legitimate
///        JTI revocations, making previously revoked tokens valid again.
///     b) Each call also rotates / revokes a refresh token row (DB write).
///
///   Notably, the handler does not short-circuit if the refresh token cookie is
///   absent — it still writes the JTI blocklist entry.  This means a caller with
///   just an access token can spam blocklist writes indefinitely.
///
/// ── Attack 2: UpdateMyProfile — unthrottled DB write ─────────────────────────
///   <c>UpdateUserProfileCommand</c> issues a DB UPDATE on every call.
///   Without rate limiting a compromised JWT can generate continuous UPDATE
///   operations against the users table for the full 15-minute token window.
///   The validator caps the <c>Name</c> field, but there is no HTTP-layer size
///   ceiling either.
///
/// ── Attack 3: GetMyProfile — unthrottled DB read ─────────────────────────────
///   <c>GetMyProfileQuery</c> issues one DB SELECT per call.  Unthrottled, a
///   polling loop (e.g., a buggy mobile client) can exhaust DB connections.
///
/// Fix:
///   Apply <c>[EnableRateLimiting("public-api")]</c> (30 req/min) to all three
///   methods.  The four existing auth endpoints (register/login/refresh/verify)
///   already carry purpose-specific policies; <c>public-api</c> is the correct
///   default for authenticated read/write endpoints that do not need their own
///   narrower policy.  Also add <c>[RequestSizeLimit(4096)]</c> to
///   <c>UpdateMyProfile</c> — the body contains only a display name; anything
///   beyond 4 KB is adversarial.
/// </summary>
public sealed class Round33SecurityRegressionTests
{
    // ── POST /api/auth/logout ── Logout ───────────────────────────────────────

    [Fact]
    public void AuthController_Logout_HasEnableRateLimitingAttribute()
    {
        // LogoutCommand writes to both the DB (refresh token revocation) and the
        // Redis JTI blocklist on every call.  Without throttling a valid access
        // token can flood both stores before the 15-min expiry.
        var method = typeof(AuthController)
            .GetMethod("Logout", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull("AuthController must expose a public Logout method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "POST /api/auth/logout must carry [EnableRateLimiting] — LogoutCommand " +
            "writes to the JTI blocklist (Redis) AND the refresh-token revocation " +
            "table (DB) on every call; without throttling a valid JWT can flood both " +
            "stores before expiry, potentially evicting legitimate JTI revocations " +
            "and making previously revoked tokens valid again");
    }

    [Fact]
    public void AuthController_Logout_UsesPublicApiPolicy()
    {
        var method = typeof(AuthController)
            .GetMethod("Logout", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be(
            "public-api",
            "Logout must use the 'public-api' policy (30 req/min); " +
            "a legitimate user logs out once — 30/min is generous while " +
            "stopping blocklist-flood attacks");
    }

    // ── GET /api/auth/me ── GetMyProfile ──────────────────────────────────────

    [Fact]
    public void AuthController_GetMyProfile_HasEnableRateLimitingAttribute()
    {
        // GetMyProfileQuery issues one DB SELECT per call.
        // Without throttling a polling loop exhausts DB connections.
        var method = typeof(AuthController)
            .GetMethod("GetMyProfile", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull("AuthController must expose a public GetMyProfile method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "GET /api/auth/me must carry [EnableRateLimiting] — issues one DB " +
            "SELECT per call; without throttling a buggy polling client or a " +
            "compromised JWT exhausts DB connections within the token window");
    }

    [Fact]
    public void AuthController_GetMyProfile_UsesPublicApiPolicy()
    {
        var method = typeof(AuthController)
            .GetMethod("GetMyProfile", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be("public-api",
            "GetMyProfile must use the 'public-api' policy (30 req/min)");
    }

    // ── PATCH /api/auth/me ── UpdateMyProfile ─────────────────────────────────

    [Fact]
    public void AuthController_UpdateMyProfile_HasEnableRateLimitingAttribute()
    {
        // UpdateUserProfileCommand issues one DB UPDATE per call.
        // Without throttling a compromised JWT generates continuous UPDATE churn.
        var method = typeof(AuthController)
            .GetMethod("UpdateMyProfile", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull("AuthController must expose a public UpdateMyProfile method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "PATCH /api/auth/me must carry [EnableRateLimiting] — issues one DB " +
            "UPDATE per call; without throttling a compromised JWT generates " +
            "unbounded UPDATE churn for the full 15-minute token lifetime");
    }

    [Fact]
    public void AuthController_UpdateMyProfile_UsesPublicApiPolicy()
    {
        var method = typeof(AuthController)
            .GetMethod("UpdateMyProfile", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be("public-api",
            "UpdateMyProfile must use the 'public-api' policy (30 req/min)");
    }

    [Fact]
    public void AuthController_UpdateMyProfile_HasRequestSizeLimitAttribute()
    {
        // Body contains only a display name; 4 KB is more than sufficient.
        // Without a size ceiling an oversized body reaches the model binder
        // before FluentValidation can reject it.
        var method = typeof(AuthController)
            .GetMethod("UpdateMyProfile", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull();

        var attr = method!.GetCustomAttribute<RequestSizeLimitAttribute>();
        attr.Should().NotBeNull(
            "PATCH /api/auth/me must carry [RequestSizeLimit] — the body " +
            "contains only a display name; anything beyond 4 KB is adversarial " +
            "and must be rejected at the HTTP layer before model binding");
    }
}

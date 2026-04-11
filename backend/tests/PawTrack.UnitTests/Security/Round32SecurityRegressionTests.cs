using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.API.Controllers;
using System.Reflection;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-32 security regression tests.
///
/// Gap: Five endpoints on <c>PetsController</c> carry <c>[Authorize]</c> but
/// have <b>zero <c>[EnableRateLimiting]</c></b>:
///
///   • <c>GET /api/pets</c>                   — <c>GetMyPets</c>
///   • <c>GET /api/pets/{id}</c>              — <c>GetPetDetail</c>
///   • <c>GET /api/pets/{id}/qr</c>           — <c>GetQrCode</c>         ← CPU-DoS vector
///   • <c>POST /api/pets/{id}/avatar-token</c> — <c>GenerateAvatarToken</c>
///   • <c>GET /api/pets/{id}/scan-history</c> — <c>GetScanHistory</c>
///
/// ── Attack 1: QR-code CPU exhaustion (GetQrCode) ──────────────────────────────
///   <c>GetQrCode</c> calls <c>IQrCodeService.GeneratePng(url)</c> synchronously
///   on <b>every request</b>.  A QR PNG generation involves:
///     ① Reed-Solomon error-correction encoding of the URL bytes
///     ② Matrix rendering with quiet-zone padding
///     ③ PNG compression (lossless, CPU-intensive at quality settings tuned for
///        mobile scanning)
///   A single authenticated account hammering this endpoint with 49 concurrent
///   connections can saturate one CPU core within seconds.  Kestrel's default
///   thread pool does not enforce CPU caps per connection — only request throughput
///   is bounded, and only by the OS scheduler.
///
///   The attack surface is widened by the fact that all pet IDs are available to
///   the owner via <c>GET /api/pets</c> (itself unthrottled), creating a
///   two-step self-supply attack on the CPU with zero external enumeration.
///
/// ── Attack 2: Avatar-token HMAC flooding (GenerateAvatarToken) ───────────────
///   <c>GenerateAvatarToken</c> executes <c>GetPetDetailQuery</c> (DB read for
///   ownership check) + <c>IAvatarTokenService.Generate</c> (HMAC-SHA256 over
///   pet GUID + expiry timestamp) on every call.  Without rate limiting a tight
///   loop:
///     ① Generates unlimited signed tokens (each valid 60 min) — if tokens are
///        revocable only on TTL expiry, flooding them wastes downstream cache/
///        validation capacity.
///     ② Generates unlimited DB reads via the ownership query.
///
/// ── Attack 3: Unthrottled list and detail reads ───────────────────────────────
///   <c>GetMyPets</c> returns all pets for the owner (unbounded SELECT by userId).
///   <c>GetPetDetail</c> and <c>GetScanHistory</c> each issue one DB read (one per
///   GUID the attacker knows from <c>GetMyPets</c>).  None are throttled — a
///   retry storm from a buggy mobile client or a compromised JWT can exhaust the
///   connection pool within the 15-minute token window.
///
/// Fix:
///   Apply <c>[EnableRateLimiting("public-api")]</c> (30 req/min) to all five
///   methods.  The <c>public-api</c> policy is already applied to the three write
///   endpoints on the same controller, making this a uniform throttle across the
///   full <c>PetsController</c>.
/// </summary>
public sealed class Round32SecurityRegressionTests
{
    // ── GET /api/pets ── GetMyPets ────────────────────────────────────────────

    [Fact]
    public void PetsController_GetMyPets_HasEnableRateLimitingAttribute()
    {
        // SELECT all pets for this user — unbounded without a throttle.
        var method = typeof(PetsController)
            .GetMethod("GetMyPets", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull("PetsController must expose a public GetMyPets method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "GET /api/pets must carry [EnableRateLimiting] — unthrottled list " +
            "queries against the pets table with no per-user cap; all pet IDs " +
            "returned here feed the QR-code CPU-DoS vector below");
    }

    [Fact]
    public void PetsController_GetMyPets_UsesPublicApiPolicy()
    {
        var method = typeof(PetsController)
            .GetMethod("GetMyPets", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be(
            "public-api",
            "GetMyPets must use the 'public-api' policy (30 req/min) for " +
            "consistency with the write endpoints on the same controller");
    }

    // ── GET /api/pets/{id} ── GetPetDetail ────────────────────────────────────

    [Fact]
    public void PetsController_GetPetDetail_HasEnableRateLimitingAttribute()
    {
        var method = typeof(PetsController)
            .GetMethod("GetPetDetail", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull("PetsController must expose a public GetPetDetail method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "GET /api/pets/{id} must carry [EnableRateLimiting] — " +
            "ownership-checked DB read on every call; without throttle a " +
            "compromised JWT can generate unbounded DB queries for its 15-min lifetime");
    }

    [Fact]
    public void PetsController_GetPetDetail_UsesPublicApiPolicy()
    {
        var method = typeof(PetsController)
            .GetMethod("GetPetDetail", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be("public-api",
            "GetPetDetail must use the 'public-api' policy (30 req/min)");
    }

    // ── GET /api/pets/{id}/qr ── GetQrCode ────────────────────────────────────

    [Fact]
    public void PetsController_GetQrCode_HasEnableRateLimitingAttribute()
    {
        // CRITICAL: qrCodeService.GeneratePng() is a CPU-intensive operation
        // (Reed-Solomon encoding + PNG compression) called synchronously on
        // every request.  Without [EnableRateLimiting] a single JWT saturates
        // a CPU core within seconds via concurrent hammering.
        var method = typeof(PetsController)
            .GetMethod("GetQrCode", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull("PetsController must expose a public GetQrCode method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "GET /api/pets/{id}/qr MUST carry [EnableRateLimiting] — " +
            "IQrCodeService.GeneratePng() is CPU-intensive (Reed-Solomon + PNG " +
            "compression) and is called synchronously on every request; without " +
            "throttling a single authenticated account can saturate a CPU core " +
            "in seconds by hammering this endpoint concurrently");
    }

    [Fact]
    public void PetsController_GetQrCode_UsesPublicApiPolicy()
    {
        var method = typeof(PetsController)
            .GetMethod("GetQrCode", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be("public-api",
            "GetQrCode must use the 'public-api' policy (30 req/min)");
    }

    // ── POST /api/pets/{id}/avatar-token ── GenerateAvatarToken ──────────────

    [Fact]
    public void PetsController_GenerateAvatarToken_HasEnableRateLimitingAttribute()
    {
        // Each call executes GetPetDetailQuery (DB ownership check) +
        // IAvatarTokenService.Generate (HMAC-SHA256).  Without throttling a
        // tight loop creates unlimited signed tokens and unlimited DB reads.
        var method = typeof(PetsController)
            .GetMethod("GenerateAvatarToken", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull("PetsController must expose a public GenerateAvatarToken method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "POST /api/pets/{id}/avatar-token must carry [EnableRateLimiting] — " +
            "each call fires GetPetDetailQuery (DB read) + HMAC-SHA256 token " +
            "generation; without throttling a compromised JWT floods the token " +
            "namespace and generates unbounded DB ownership queries");
    }

    [Fact]
    public void PetsController_GenerateAvatarToken_UsesPublicApiPolicy()
    {
        var method = typeof(PetsController)
            .GetMethod("GenerateAvatarToken", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be("public-api",
            "GenerateAvatarToken must use the 'public-api' policy (30 req/min)");
    }

    // ── GET /api/pets/{id}/scan-history ── GetScanHistory ────────────────────

    [Fact]
    public void PetsController_GetScanHistory_HasEnableRateLimitingAttribute()
    {
        var method = typeof(PetsController)
            .GetMethod("GetScanHistory", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull("PetsController must expose a public GetScanHistory method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "GET /api/pets/{id}/scan-history must carry [EnableRateLimiting] — " +
            "ownership-checked DB read; without throttle a compromised JWT can " +
            "generate unbounded queries for the full 15-min token window");
    }

    [Fact]
    public void PetsController_GetScanHistory_UsesPublicApiPolicy()
    {
        var method = typeof(PetsController)
            .GetMethod("GetScanHistory", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be("public-api",
            "GetScanHistory must use the 'public-api' policy (30 req/min)");
    }
}

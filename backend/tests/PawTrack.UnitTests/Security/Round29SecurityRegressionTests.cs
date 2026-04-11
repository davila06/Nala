using FluentAssertions;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.API.Controllers;
using System.Reflection;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-29 security regression tests.
///
/// Gap: Three <c>GET</c> endpoints on <c>LostPetsController</c> carry
/// <c>[Authorize]</c> but <b>zero <c>[EnableRateLimiting]</c></b>:
///
///   • <c>GET /api/lost-pets/{id}</c>          — <c>GetById</c>
///   • <c>GET /api/lost-pets/by-pet/{petId}</c> — <c>GetActiveByPet</c>
///   • <c>GET /api/lost-pets/{id}/case</c>      — <c>GetCaseRoom</c>
///
/// A 15-minute JWT is sufficient to call any of these endpoints with no
/// per-user or per-IP constraint beyond what ASP.NET Core can handle by default
/// (≈ global Kestrel connection limits, which are not entity-level guards).
///
/// ── Attack 1: Enumeration via GetById / GetActiveByPet ────────────────────────
///   <c>GetById</c> and <c>GetActiveByPet</c> each issue one DB query per call.
///   The lost-pet event IDs are publicly enumerable via <c>GET /api/public/map</c>
///   (unauthenticated).  With a valid JWT an attacker can pull the full payload of
///   every active lost-pet report — including <c>PublicMessage</c>, last-seen GPS,
///   and <c>ContactName</c> — at unlimited speed.
///
/// ── Attack 2: GetCaseRoom — the most expensive query in the domain ─────────────
///   <c>GetCaseRoomQuery</c> assembles a full aggregate on every call:
///     ① The <c>LostPetEvent</c> entity (DB read)
///     ② All linked <c>Sighting</c> records ranked by priority score (DB read +
///        in-memory sort)
///     ③ The anonymised notification-dispatch log for the event (DB read)
///   This is three DB operations per request.  Without a rate limit, the event
///   owner's valid JWT can become a DoS vector against the three underlying tables
///   — either accidentally (e.g., a mobile client caught in a retry storm) or as
///   an intentional directed attack on the aggregate query.
///
/// Fix:
///   Apply <c>[EnableRateLimiting("public-api")]</c> (30 req/min) to all three
///   GET endpoints.  The <c>public-api</c> policy is already applied to the
///   <b>write</b> endpoints on the same controller (<c>POST</c> and <c>PUT</c>),
///   making this a uniform throttle across the full controller.
/// </summary>
public sealed class Round29SecurityRegressionTests
{
    // ── GET /api/lost-pets/{id} ── GetById ────────────────────────────────────

    [Fact]
    public void LostPetsController_GetById_HasEnableRateLimitingAttribute()
    {
        var method = typeof(LostPetsController)
            .GetMethod("GetById", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull(
            "LostPetsController must expose a public GetById method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "GET /api/lost-pets/{id} must carry [EnableRateLimiting] — active " +
            "event IDs are publicly enumerable from /api/public/map; without " +
            "throttling a valid JWT can harvest every lost-pet payload at " +
            "unlimited speed");
    }

    [Fact]
    public void LostPetsController_GetById_UsesPublicApiPolicy()
    {
        var method = typeof(LostPetsController)
            .GetMethod("GetById", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be(
            "public-api",
            "GetById must use the 'public-api' policy (30 req/min) for " +
            "consistency with the write endpoints on the same controller");
    }

    // ── GET /api/lost-pets/by-pet/{petId} ── GetActiveByPet ──────────────────

    [Fact]
    public void LostPetsController_GetActiveByPet_HasEnableRateLimitingAttribute()
    {
        var method = typeof(LostPetsController)
            .GetMethod("GetActiveByPet", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull(
            "LostPetsController must expose a public GetActiveByPet method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "GET /api/lost-pets/by-pet/{petId} must carry [EnableRateLimiting] — " +
            "without throttling a valid JWT can poll every pet's lost status at " +
            "unlimited speed, revealing real-time pet location data");
    }

    [Fact]
    public void LostPetsController_GetActiveByPet_UsesPublicApiPolicy()
    {
        var method = typeof(LostPetsController)
            .GetMethod("GetActiveByPet", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be(
            "public-api",
            "GetActiveByPet must use the 'public-api' policy (30 req/min)");
    }

    // ── GET /api/lost-pets/{id}/case ── GetCaseRoom ───────────────────────────

    [Fact]
    public void LostPetsController_GetCaseRoom_HasEnableRateLimitingAttribute()
    {
        // GetCaseRoom is the most expensive query in the domain — it executes
        // THREE DB operations (event + ranked sightings + notification log)
        // on every call. Without [EnableRateLimiting] a retry storm from any
        // owner's mobile client can cause a directed DoS on the three underlying tables.
        var method = typeof(LostPetsController)
            .GetMethod("GetCaseRoom", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull(
            "LostPetsController must expose a public GetCaseRoom method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "GET /api/lost-pets/{id}/case must carry [EnableRateLimiting] — " +
            "this is the most DB-intensive query in the domain (3 reads per call: " +
            "event + ranked sightings + notification log); without throttling a " +
            "retry storm or accidental polling loop can DoS the aggregation tables");
    }

    [Fact]
    public void LostPetsController_GetCaseRoom_UsesPublicApiPolicy()
    {
        var method = typeof(LostPetsController)
            .GetMethod("GetCaseRoom", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be(
            "public-api",
            "GetCaseRoom must use the 'public-api' policy (30 req/min)");
    }
}

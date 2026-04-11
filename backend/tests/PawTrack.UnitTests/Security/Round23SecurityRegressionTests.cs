using FluentAssertions;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.API.Controllers;
using System.Reflection;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-23 security regression tests.
///
/// Gap: <c>LostPetsController</c> has <c>[Authorize]</c> at class level but
/// carries <b>zero <c>[EnableRateLimiting]</c> attributes</b> on any of its six
/// endpoints.
///
/// The most critical target is:
///   <c>GET /api/lost-pets/{id}/contact</c>
///
/// Attack vector (PII bulk-harvesting):
///   1. Active lost-pet event IDs are publicly enumerable via
///      <c>GET /api/public/map</c> (returns all active events with their GUIDs,
///      last-seen coordinates, etc.) — no authentication required.
///   2. <c>GET /api/lost-pets/{id}/contact</c> is available to ANY authenticated
///      user (by design — a finder must be able to call the owner).  There is no
///      ownership check and no rate limit.
///   3. An attacker registers a free account, obtains a JWT, then:
///         for eventId in map_event_ids:
///             phone = GET /api/lost-pets/{eventId}/contact → ContactPhone
///      Because there is no rate limit, all phone numbers can be harvested in
///      seconds.  The result is a complete database of owner phone numbers
///      correlated with their lost pets and last-seen GPS locations.
///
/// Secondary targets (write-endpoint DoS):
///   - <c>POST /api/lost-pets</c>: creates a new report, writes DB, dispatches
///     notifications, uploads a photo to Blob Storage.  Without a rate limit a
///     compromised account can spam the report pipeline.
///   - <c>PUT /api/lost-pets/{id}/status</c>: triggers the LostPetStatus state
///     machine and may dispatch cross-module MediatR events.  Without a rate
///     limit a compromised account can cycle status transitions indefinitely.
///
/// Fix:
///   • Register a new <c>"contact-lookup"</c> fixed-window policy in
///     <c>Program.cs</c> (10 req/min — tighter than "public-api" because each
///     response discloses a real phone number).
///   • Apply <c>[EnableRateLimiting("contact-lookup")]</c> to <c>GetContact</c>.
///   • Apply <c>[EnableRateLimiting("public-api")]</c> to the two write endpoints
///     (<c>ReportLostPet</c> and <c>UpdateStatus</c>) and to the two read
///     endpoints that have no limiter (<c>GetById</c>, <c>GetActiveByPet</c>,
///     <c>GetCaseRoom</c>).
/// </summary>
public sealed class Round23SecurityRegressionTests
{
    // ── GET /api/lost-pets/{id}/contact — PII phone-number harvest ────────────

    [Fact]
    public void LostPetsController_GetContact_HasEnableRateLimitingAttribute()
    {
        // Without a rate limit any authenticated user can enumerate all active
        // lost-pet event IDs (public map) and scrape every owner's phone number
        // in one tight loop — classic bulk PII harvesting attack.
        var method = typeof(LostPetsController)
            .GetMethod("GetContact", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull(
            "LostPetsController must expose a public GetContact method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "GET /api/lost-pets/{id}/contact returns ContactPhone (PII) to any " +
            "authenticated user; without [EnableRateLimiting] an attacker can " +
            "correlate all public map event IDs and harvest every owner's phone " +
            "number in bulk");
    }

    [Fact]
    public void LostPetsController_GetContact_UsesContactLookupPolicy()
    {
        // "contact-lookup" must be stricter than "public-api" (10 vs 30 /min)
        // because each response discloses a real phone number — not just metadata.
        var method = typeof(LostPetsController)
            .GetMethod("GetContact", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be(
            "contact-lookup",
            "the rate-limit policy for the contact endpoint must be 'contact-lookup' " +
            "(10 req/min) to match the stricter policy registered in Program.cs; " +
            "'public-api' (30/min) is too generous for a phone-number disclosure endpoint");
    }

    // ── POST /api/lost-pets — write DoS ───────────────────────────────────────

    [Fact]
    public void LostPetsController_ReportLostPet_HasEnableRateLimitingAttribute()
    {
        // Each call creates a DB record, uploads a photo to Blob Storage, and
        // dispatches notifications.  Without a rate limit a compromised account
        // can flood the report pipeline, exhausting storage and notification quotas.
        var method = typeof(LostPetsController)
            .GetMethod("ReportLostPet", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull(
            "LostPetsController must expose a public ReportLostPet method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "POST /api/lost-pets must carry [EnableRateLimiting] — each call " +
            "writes a DB record, may upload a photo, and dispatches notifications; " +
            "without a rate limit a compromised account can exhaust storage and " +
            "notification quotas");
    }

    [Fact]
    public void LostPetsController_ReportLostPet_UsesPublicApiPolicy()
    {
        var method = typeof(LostPetsController)
            .GetMethod("ReportLostPet", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be(
            "public-api",
            "POST /api/lost-pets must use 'public-api' (30/min) — generous enough " +
            "for legitimate use but sufficient to block DoS via report flooding");
    }

    // ── PUT /api/lost-pets/{id}/status — state-machine DoS ───────────────────

    [Fact]
    public void LostPetsController_UpdateStatus_HasEnableRateLimitingAttribute()
    {
        // Each call triggers the LostPetStatus state machine and may emit
        // cross-module domain events.  Without a rate limit a compromised account
        // can cycle status transitions at maximum DB write rate.
        var method = typeof(LostPetsController)
            .GetMethod("UpdateStatus", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull(
            "LostPetsController must expose a public UpdateStatus method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "PUT /api/lost-pets/{id}/status must carry [EnableRateLimiting] — " +
            "each call exercises the LostPetStatus state machine and may dispatch " +
            "domain events; without rate limiting a compromised account can spam " +
            "status changes indefinitely");
    }

    [Fact]
    public void LostPetsController_UpdateStatus_UsesPublicApiPolicy()
    {
        var method = typeof(LostPetsController)
            .GetMethod("UpdateStatus", BindingFlags.Public | BindingFlags.Instance);

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull();
        attr!.PolicyName.Should().Be(
            "public-api",
            "PUT /api/lost-pets/{id}/status must use 'public-api' (30/min) for " +
            "consistency with other authenticated write endpoints");
    }
}

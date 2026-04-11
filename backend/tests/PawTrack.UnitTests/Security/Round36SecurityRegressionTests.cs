using FluentAssertions;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.API.Controllers;
using System.Reflection;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-36 security regression test.
///
/// Gap: <c>GET /api/sightings/pet/{petId}</c> — <c>GetSightingsByPet</c> — is
/// behind <c>[Authorize]</c> but carries <b>no <c>[EnableRateLimiting]</c></b>.
///
/// ── Attack vector ────────────────────────────────────────────────────────────
///   Active <c>petId</c> GUIDs are publicly discoverable via the public map
///   (<c>GET /api/public/map</c>) as part of lost-pet event records.  A valid
///   JWT (15-min access-token window) is sufficient to hammer this endpoint:
///
///     for each petId in publicMapResults:
///         GET /api/sightings/pet/{petId}   // no throttle — fires one full DB
///                                          // SELECT per call, joining Sightings
///                                          // and verifying PetOwnership
///
///   This creates an unbounded DB read amplification path.  The endpoint performs
///   an ownership check plus a sightings SELECT; under sustained traffic each
///   call holds a DB connection for the duration of both queries.
///
/// ── All sibling endpoints in SightingsController are already rate-limited ────
///   • <c>POST /api/sightings</c>                       — "sightings" policy ✓
///   • <c>POST /api/sightings/visual-match</c>          — "sightings" policy ✓
///   • <c>POST /api/sightings/{id}/visual-match</c>     — "sightings" policy ✓
///   <c>GetSightingsByPet</c> is the sole exception, making it an easy-to-miss
///   but individually exploitable gap.
///
/// Fix:
///   Apply <c>[EnableRateLimiting("public-api")]</c> (30 req/min) to
///   <c>GetSightingsByPet</c>.
/// </summary>
public sealed class Round36SecurityRegressionTests
{
    // ── GET /api/sightings/pet/{petId} — GetSightingsByPet ───────────────────

    [Fact]
    public void SightingsController_GetSightingsByPet_HasEnableRateLimitingAttribute()
    {
        var method = typeof(SightingsController)
            .GetMethod("GetSightingsByPet", BindingFlags.Public | BindingFlags.Instance);

        method.Should().NotBeNull("SightingsController must expose a public GetSightingsByPet method");

        var attr = method!.GetCustomAttribute<EnableRateLimitingAttribute>();
        attr.Should().NotBeNull(
            "GET /api/sightings/pet/{petId} issues a petOwnership check + sightings SELECT per call. " +
            "PetIds are discoverable from the public map, so an authenticated user can generate " +
            "unbounded DB reads with no throttle. [EnableRateLimiting] is required.");
    }

    [Fact]
    public void SightingsController_GetSightingsByPet_UsesPublicApiPolicy()
    {
        var method = typeof(SightingsController)
            .GetMethod("GetSightingsByPet", BindingFlags.Public | BindingFlags.Instance)!;

        var attr = method.GetCustomAttribute<EnableRateLimitingAttribute>()!;
        attr.PolicyName.Should().Be("public-api",
            "GetSightingsByPet must use the 'public-api' policy (30 req/min) — " +
            "consistent with all other authenticated read endpoints in the application");
    }
}

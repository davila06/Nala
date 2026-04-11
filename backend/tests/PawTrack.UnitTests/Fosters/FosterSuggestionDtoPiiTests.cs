using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.Application.Fosters.DTOs;
using PawTrack.API.Controllers;

namespace PawTrack.UnitTests.Fosters;

/// <summary>
/// Guards that <see cref="FosterSuggestionDto"/> does NOT expose volunteer GUIDs
/// or make the suggestions endpoint callable by anonymous users.
///
/// Attack vector being prevented:
///   1. An anonymous caller creates a found-pet report at any GPS coordinate.
///   2. They call GET /api/fosters/suggestions/from-found-report/{id}.
///   3. The response contains {UserId, VolunteerName} for every foster within 3km.
///   4. By sweeping coordinates, they enumerate ALL volunteer GUIDs + real names.
/// </summary>
public sealed class FosterSuggestionDtoPiiTests
{
    // ── DTO structure ─────────────────────────────────────────────────────────

    [Fact]
    public void FosterSuggestionDto_HasNoUserIdProperty()
    {
        // The raw GUID lets any caller build a cross-event volunteer profile.
        // This test fails until the property is removed.
        typeof(FosterSuggestionDto)
            .GetProperty("UserId")
            .Should().BeNull(
                "FosterSuggestionDto must not expose volunteer GUIDs — " +
                "any authenticated caller could enumerate all volunteers by sweeping GPS coordinates");
    }

    [Fact]
    public void FosterSuggestionDto_HasIsSpeciesMatchProperty()
    {
        // Confirms non-sensitive fields are retained after the UserId removal.
        typeof(FosterSuggestionDto)
            .GetProperty(nameof(FosterSuggestionDto.SpeciesMatch))
            .Should().NotBeNull("SpeciesMatch is safe to expose and must be preserved");
    }

    [Fact]
    public void FosterSuggestionDto_HasDistanceMetresProperty()
    {
        typeof(FosterSuggestionDto)
            .GetProperty(nameof(FosterSuggestionDto.DistanceMetres))
            .Should().NotBeNull("DistanceMetres is safe to expose and must be preserved");
    }

    // ── Controller authorization ───────────────────────────────────────────────

    [Fact]
    public void FostersController_GetSuggestions_DoesNotAllowAnonymous()
    {
        // The AllowAnonymous attribute on GetSuggestions lets anyone call it,
        // which enables silent volunteer enumeration without an account.
        // After the fix, the method must NOT carry [AllowAnonymous].
        var method = typeof(FostersController)
            .GetMethod(nameof(FostersController.GetSuggestions));

        method.Should().NotBeNull();

        var hasAllowAnonymous = method!
            .GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true)
            .Any();

        hasAllowAnonymous.Should().BeFalse(
            "GET /api/fosters/suggestions/... must require authentication to prevent " +
            "anonymous enumeration of volunteer names and identities");
    }

    [Fact]
    public void FostersController_GetSuggestions_HasAuthorizeAttribute()
    {
        // Positive check: the method must carry [Authorize] (or inherit it from the class).
        var controllerType = typeof(FostersController);
        var method = controllerType.GetMethod(nameof(FostersController.GetSuggestions));

        method.Should().NotBeNull();

        // Check direct method attribute OR the controller-level attribute.
        var methodHasAuthorize = method!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Any();

        var classHasAuthorize = controllerType
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Any();

        (methodHasAuthorize || classHasAuthorize).Should().BeTrue(
            "GetSuggestions must be reachable only by authenticated users");
    }

    // ── Rate limiting — GenerateHandoverCode ──────────────────────────────────

    [Fact]
    public void HandoverController_GenerateCode_HasRateLimitAttribute()
    {
        // POST /api/lost-pets/{id}/handover/code has no rate limiter.
        // An attacker with a valid owner token can cycle codes at will,
        // invalidating legitimate handovers mid-flow.
        var method = typeof(HandoverController)
            .GetMethod(nameof(HandoverController.GenerateCode));

        method.Should().NotBeNull();

        var hasRateLimit = method!
            .GetCustomAttributes(typeof(EnableRateLimitingAttribute), inherit: false)
            .Any();

        hasRateLimit.Should().BeTrue(
            "GenerateCode must be rate-limited to prevent rapid code-cycling attacks");
    }
}

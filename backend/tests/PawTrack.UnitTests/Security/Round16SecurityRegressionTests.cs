using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PawTrack.API.Controllers;
using PawTrack.Application.Incentives.DTOs;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-16 security regression tests.
///
/// Gap: <c>ContributorScoreDto</c> exposes <c>UserId</c> (Guid serialised as string)
/// in the public leaderboard response (<c>GET /api/incentives/leaderboard</c>).
///
/// Attack vector:
///   1. Anonymous caller hits the public leaderboard endpoint (no auth required,
///      rate-limited at 30 req/min — generous enough for scripted enumeration).
///   2. Response body contains {UserId, OwnerName, ...} for every top-50 contributor.
///   3. By correlating UserId GUIDs with UUIDs exposed in other endpoints the
///      attacker can build a cross-table profile: user identity → real name →
///      volunteer history → home-area from sighting GPS data.
///
/// Fix: Remove <c>UserId</c> from <c>ContributorScoreDto</c>.  The leaderboard
/// purpose is to show score + badge; no identity claim needs to be exposed.
/// </summary>
public sealed class Round16SecurityRegressionTests
{
    // ── ContributorScoreDto structure ─────────────────────────────────────────

    [Fact]
    public void ContributorScoreDto_HasNoUserIdProperty()
    {
        // The UserId GUID lets any unauthenticated caller map real names to internal
        // account identifiers via the public leaderboard.  Must be removed.
        typeof(ContributorScoreDto)
            .GetProperty("UserId")
            .Should().BeNull(
                "ContributorScoreDto must not expose user GUIDs in the public leaderboard — " +
                "any anonymous caller can map OwnerName → UserId and cross-reference with " +
                "other API endpoints to build full user profiles");
    }

    [Fact]
    public void ContributorScoreDto_PreservesNonSensitiveFields()
    {
        typeof(ContributorScoreDto).GetProperty(nameof(ContributorScoreDto.OwnerName))
            .Should().NotBeNull("OwnerName (display name) is safe to show on the leaderboard");

        typeof(ContributorScoreDto).GetProperty(nameof(ContributorScoreDto.ReunificationCount))
            .Should().NotBeNull("ReunificationCount must be preserved");

        typeof(ContributorScoreDto).GetProperty(nameof(ContributorScoreDto.Badge))
            .Should().NotBeNull("Badge must be preserved");

        typeof(ContributorScoreDto).GetProperty(nameof(ContributorScoreDto.TotalPoints))
            .Should().NotBeNull("TotalPoints must be preserved");
    }
}

using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using PawTrack.API.Controllers;
using PawTrack.Application.LostPets.DTOs;
using PawTrack.Application.LostPets.Queries.GetSearchZones;
using PawTrack.Application.LostPets.Queries.IsSearchParticipant;
using PawTrack.Domain.Common;
using System.Security.Claims;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-57 security regression tests.
///
/// Gap: <c>GET /api/search-coordination/{lostEventId}/zones</c> is protected only
/// by <c>[Authorize]</c> (any authenticated user may call it), while the SignalR
/// hub methods — <c>JoinSearch</c>, <c>ClaimZone</c>, <c>ClearZone</c>,
/// <c>ReleaseZone</c>, <c>UpdateLocation</c> — all gate on
/// <c>IsSearchParticipantQuery</c> first:
///
///   <code>
///   // Hub (correct)
///   var check = await sender.Send(new IsSearchParticipantQuery(lostEventId, userId));
///   if (check.IsFailure || !check.Value) return; // silently deny
///
///   // REST endpoint (missing the gate — BOLA)
///   public async Task&lt;IActionResult&gt; GetZones(Guid lostEventId, ...)
///   {
///       var result = await sender.Send(new GetSearchZonesQuery(lostEventId), ...);
///       ...
///   }
///   </code>
///
/// The consequence is a BOLA (Broken Object-Level Authorisation) vulnerability:
/// any authenticated user with a lostEventId obtained from public map data can
/// enumerate the 49-zone grid, infer volunteer assignment patterns through repeated
/// polling, and build a real-time operational picture without joining the search.
///
/// Fix:
///   Add an <c>IsSearchParticipantQuery</c> check at the top of <c>GetZones</c>.
///   If the caller is not a confirmed participant, return <c>Forbid()</c>.
/// </summary>
public sealed class Round57SecurityRegressionTests
{
    private readonly Guid _lostEventId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    // ── Non-participant is forbidden ─────────────────────────────────────────

    [Fact]
    public async Task GetZones_WhenCallerIsNotSearchParticipant_ReturnsForbid()
    {
        // Arrange — sender returns false for the participant check
        var sender = Substitute.For<ISender>();
        sender.Send(
                Arg.Is<IsSearchParticipantQuery>(q =>
                    q.LostEventId == _lostEventId && q.UserId == _userId),
                Arg.Any<CancellationToken>())
              .Returns(Result.Success(false)); // authenticated but not a participant

        var controller = BuildController(sender, _userId);

        // Act
        var result = await controller.GetZones(_lostEventId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ForbidResult>(
            "a caller who is not a confirmed search participant must not be able to " +
            "enumerate the zone grid — the REST endpoint must apply the same " +
            "IsSearchParticipantQuery gate that the SignalR hub methods use");
    }

    // ── Participant sees the zones ────────────────────────────────────────────

    [Fact]
    public async Task GetZones_WhenCallerIsSearchParticipant_ReturnsOk()
    {
        // Arrange — participant check passes, zones query returns data
        var sender = Substitute.For<ISender>();
        sender.Send(
                Arg.Is<IsSearchParticipantQuery>(q =>
                    q.LostEventId == _lostEventId && q.UserId == _userId),
                Arg.Any<CancellationToken>())
              .Returns(Result.Success(true));

        sender.Send(
                Arg.Is<GetSearchZonesQuery>(q => q.LostPetEventId == _lostEventId),
                Arg.Any<CancellationToken>())
              .Returns(Result.Success<IReadOnlyList<SearchZoneDto>>([]));  

        var controller = BuildController(sender, _userId);

        // Act
        var result = await controller.GetZones(_lostEventId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>(
            "a confirmed search participant is authorised to view the zone grid");
    }

    // ── Unauthenticated caller (no identity claim) ────────────────────────────

    [Fact]
    public async Task GetZones_WhenCallerHasNoIdentityClaim_ReturnsUnauthorized()
    {
        // Arrange — controller with no NameIdentifier claim
        var sender = Substitute.For<ISender>();
        var controller = BuildController(sender, userId: null);

        // Act
        var result = await controller.GetZones(_lostEventId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>(
            "a request without a valid identity claim must be rejected with 401");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SearchCoordinationController BuildController(
        ISender sender, Guid? userId)
    {
        var controller = new SearchCoordinationController(sender);

        ClaimsPrincipal principal;
        if (userId.HasValue)
        {
            var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()),
            ], "Test");
            principal = new ClaimsPrincipal(identity);
        }
        else
        {
            principal = new ClaimsPrincipal(new ClaimsIdentity()); // no claims
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal },
        };

        return controller;
    }
}

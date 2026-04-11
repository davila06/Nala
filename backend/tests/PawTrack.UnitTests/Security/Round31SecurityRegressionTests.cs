using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using PawTrack.API.Hubs;
using PawTrack.Application.LostPets.Commands.ClaimZone;
using PawTrack.Application.LostPets.Commands.ClearZone;
using PawTrack.Application.LostPets.Commands.ReleaseZone;
using PawTrack.Application.LostPets.Queries.IsSearchParticipant;
using PawTrack.Domain.Common;
using System.Security.Claims;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-31 security regression tests.
///
/// Gap: Round 28 introduced a participant gate on <c>JoinSearch</c>, but the three
/// zone-mutation hub methods have <b>zero participant verification</b>:
///
///   • <c>ClaimZone(lostEventId, zoneId)</c>
///   • <c>ClearZone(lostEventId, zoneId)</c>
///   • <c>ReleaseZone(lostEventId, zoneId)</c>
///
/// They are reachable by <b>any authenticated user who simply calls them over the
/// SignalR connection</b> — they do NOT need to have joined the group first.
/// SignalR hub methods are not group-scoped; they are socket-level calls available
/// to any connected client.
///
/// ── Zone-Paralysis Attack ─────────────────────────────────────────────────────
///   1. Attacker creates a free PawTrack account (1 API call).
///   2. Calls <c>GET /api/search-coordination/{eventId}/zones</c> to enumerate
///      every zone ID for the active search (rate-limited but public endpoint).
///   3. Calls <c>ClaimZone(lostEventId, zoneId)</c> for all 49 zones.
///      <c>SearchZone.TryClaim</c> only checks <c>Status == Free</c> — it never
///      verifies that the caller belongs to the search.
///   4. All 49 zones are now marked <c>IsAssigned = true</c> under the attacker's
///      account.  Every legitimate volunteer who opens the map sees "all zones
///      taken" and the real-time coordinated search collapses.
///   5. Because <c>TryClear</c> and <c>TryRelease</c> check
///      <c>AssignedToUserId == userId</c>, only the attacker can un-claim their
///      zones — giving them persistent control over the search grid for the
///      lifetime of the access token (15 min per rotation, repeatable).
///
/// ── Why JoinSearch fix alone is insufficient ───────────────────────────────────
///   The Round-28 gate prevents the attacker from receiving GPS broadcasts, but
///   <b>hub method calls are independent of group membership</b>.  A client that
///   never calls <c>JoinSearch</c> can still invoke <c>ClaimZone</c> directly.
///
/// Fix:
///   Add an <c>IsSearchParticipantQuery</c> check at the top of all three zone
///   mutation methods — identical to the pattern introduced in Round 28.
///   Non-participants receive a silent return (no error message = no info leak).
/// </summary>
public sealed class Round31SecurityRegressionTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (SearchCoordinationHub hub, ISender sender) BuildHub(
        Guid userId,
        bool isParticipant)
    {
        var sender = Substitute.For<ISender>();

        // Participant gate response
        sender.Send(Arg.Any<IsSearchParticipantQuery>(), Arg.Any<CancellationToken>())
              .Returns(Task.FromResult(Result.Success(isParticipant)));

        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            "Bearer"));

        var context = Substitute.For<HubCallerContext>();
        context.ConnectionId.Returns("test-connection");
        context.User.Returns(user);

        var clients = Substitute.For<IHubCallerClients>();
        var groupProxy = Substitute.For<IClientProxy>();
        clients.Group(Arg.Any<string>()).Returns(groupProxy);

        var hub = new SearchCoordinationHub(sender);
        hub.Context = context;
        hub.Clients = clients;

        return (hub, sender);
    }

    // ── ClaimZone: non-participant must be silently denied ────────────────────

    [Fact]
    public async Task SearchCoordinationHub_ClaimZone_WhenUserIsNotParticipant_DoesNotForwardToCommand()
    {
        // Arrange — attacker is NOT a participant in this search.
        var userId = Guid.NewGuid();
        var lostEventId = Guid.NewGuid();
        var zoneId = Guid.NewGuid();

        var (hub, sender) = BuildHub(userId, isParticipant: false);

        // Act
        await hub.ClaimZone(lostEventId, zoneId);

        // Assert — the command must never be dispatched.
        await sender.DidNotReceive()
                    .Send(Arg.Any<ClaimZoneCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchCoordinationHub_ClaimZone_WhenUserIsParticipant_ForwardsToCommand()
    {
        // Arrange — legitimate participant (owner or finder with chat thread).
        var userId = Guid.NewGuid();
        var lostEventId = Guid.NewGuid();
        var zoneId = Guid.NewGuid();

        var (hub, sender) = BuildHub(userId, isParticipant: true);

        // ClaimZoneCommand returns failure (zone already taken) — we only care
        // that the command WAS dispatched, not its outcome.
        sender.Send(Arg.Any<ClaimZoneCommand>(), Arg.Any<CancellationToken>())
              .Returns(Task.FromResult(Result.Failure<PawTrack.Application.LostPets.DTOs.SearchZoneDto>("Zone already taken.")));

        // Act
        await hub.ClaimZone(lostEventId, zoneId);

        // Assert — command is dispatched exactly once.
        await sender.Received(1)
                    .Send(Arg.Any<ClaimZoneCommand>(), Arg.Any<CancellationToken>());
    }

    // ── ClearZone: non-participant must be silently denied ────────────────────

    [Fact]
    public async Task SearchCoordinationHub_ClearZone_WhenUserIsNotParticipant_DoesNotForwardToCommand()
    {
        var userId = Guid.NewGuid();
        var lostEventId = Guid.NewGuid();
        var zoneId = Guid.NewGuid();

        var (hub, sender) = BuildHub(userId, isParticipant: false);

        // Act
        await hub.ClearZone(lostEventId, zoneId);

        // Assert
        await sender.DidNotReceive()
                    .Send(Arg.Any<ClearZoneCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchCoordinationHub_ClearZone_WhenUserIsParticipant_ForwardsToCommand()
    {
        var userId = Guid.NewGuid();
        var lostEventId = Guid.NewGuid();
        var zoneId = Guid.NewGuid();

        var (hub, sender) = BuildHub(userId, isParticipant: true);

        sender.Send(Arg.Any<ClearZoneCommand>(), Arg.Any<CancellationToken>())
              .Returns(Task.FromResult(Result.Failure<PawTrack.Application.LostPets.DTOs.SearchZoneDto>("Not your zone.")));

        // Act
        await hub.ClearZone(lostEventId, zoneId);

        // Assert
        await sender.Received(1)
                    .Send(Arg.Any<ClearZoneCommand>(), Arg.Any<CancellationToken>());
    }

    // ── ReleaseZone: non-participant must be silently denied ──────────────────

    [Fact]
    public async Task SearchCoordinationHub_ReleaseZone_WhenUserIsNotParticipant_DoesNotForwardToCommand()
    {
        var userId = Guid.NewGuid();
        var lostEventId = Guid.NewGuid();
        var zoneId = Guid.NewGuid();

        var (hub, sender) = BuildHub(userId, isParticipant: false);

        // Act
        await hub.ReleaseZone(lostEventId, zoneId);

        // Assert
        await sender.DidNotReceive()
                    .Send(Arg.Any<ReleaseZoneCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchCoordinationHub_ReleaseZone_WhenUserIsParticipant_ForwardsToCommand()
    {
        var userId = Guid.NewGuid();
        var lostEventId = Guid.NewGuid();
        var zoneId = Guid.NewGuid();

        var (hub, sender) = BuildHub(userId, isParticipant: true);

        sender.Send(Arg.Any<ReleaseZoneCommand>(), Arg.Any<CancellationToken>())
              .Returns(Task.FromResult(Result.Failure<PawTrack.Application.LostPets.DTOs.SearchZoneDto>("Zone not taken.")));

        // Act
        await hub.ReleaseZone(lostEventId, zoneId);

        // Assert
        await sender.Received(1)
                    .Send(Arg.Any<ReleaseZoneCommand>(), Arg.Any<CancellationToken>());
    }
}

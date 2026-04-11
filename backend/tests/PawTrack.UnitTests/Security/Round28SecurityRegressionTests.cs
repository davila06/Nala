using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using PawTrack.API.Hubs;
using PawTrack.Application.LostPets.Queries.IsSearchParticipant;
using PawTrack.Domain.Common;
using System.Security.Claims;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-28 security regression tests.
///
/// Gap: <c>SearchCoordinationHub.JoinSearch(Guid lostEventId)</c> adds the caller
/// to the SignalR broadcast group for <paramref name="lostEventId"/> without any
/// verification that the caller is actually a participant in that search.
///
/// Active <c>lostEventId</c> GUIDs are publicly enumerable via
/// <c>GET /api/public/map</c> (unauthenticated) — no crawling required.
///
/// Once inside the group, a caller receives every <c>LocationUpdated</c> event,
/// which carries real-time GPS positions of all other volunteers who have called
/// <c>UpdateLocation</c>. The only identifier attached to each coordinate broadcast
/// is the ephemeral <c>ConnectionId</c>, but combined with sighting data or chat
/// threads the coordinates can be linked back to real users.
///
/// Attack vector:
///   1. Attacker registers a free PawTrack account (no email verification abuse
///      needed — email is required but the round-3 limit is 5/10 min per IP,
///      which is trivially satisfied from N IPs or a VPN rotation).
///   2. Attacker calls <c>GET /api/public/map</c> → enumerates active lost-pet
///      event IDs (this endpoint is unauthenticated and rate-limited at 30/min).
///   3. With a valid JWT the attacker connects to
///      <c>/hubs/search-coordination</c> and calls
///      <c>JoinSearch(lostEventId)</c> for every active search.
///   4. Attacker receives all <c>LocationUpdated</c> events, building a real-time
///      map of every volunteer who opted into GPS sharing — across ALL active searches.
///
/// Without the fix:
///   A single authenticated account (obtainable by anyone) can passively surveil
///   the real-world movements of every field volunteer in the system.
///
/// Fix:
///   Before calling <c>Groups.AddToGroupAsync</c>, dispatch
///   <c>IsSearchParticipantQuery(lostEventId, userId)</c> via MediatR.
///   The handler grants access only to the event owner or a user with an active
///   chat thread for the event (i.e., they are an engaged finder/rescuer).
///   Non-participants receive a silent denial — no error response to avoid info leak.
/// </summary>
public sealed class Round28SecurityRegressionTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (SearchCoordinationHub hub, IGroupManager groups) BuildHub(
        ISender sender,
        Guid userId)
    {
        var groups = Substitute.For<IGroupManager>();

        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            "Bearer"));

        var context = Substitute.For<HubCallerContext>();
        context.ConnectionId.Returns("test-connection-id");
        context.User.Returns(user);

        var hub = new SearchCoordinationHub(sender);
        hub.Groups = groups;
        hub.Context = context;

        return (hub, groups);
    }

    // ── JoinSearch: non-participant must be silently denied ───────────────────

    [Fact]
    public async Task SearchCoordinationHub_JoinSearch_WhenUserIsNotParticipant_DoesNotAddToGroup()
    {
        // Arrange — attacker has a valid JWT but is NOT involved in this search.
        var userId = Guid.NewGuid();
        var lostEventId = Guid.NewGuid();

        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<IsSearchParticipantQuery>(), Arg.Any<CancellationToken>())
              .Returns(Task.FromResult(Result.Success(false)));

        var (hub, groups) = BuildHub(sender, userId);

        // Act
        await hub.JoinSearch(lostEventId);

        // Assert — attacker must NOT be added to the GPS broadcast group.
        await groups.DidNotReceive()
                    .AddToGroupAsync(
                        Arg.Any<string>(),
                        Arg.Any<string>(),
                        Arg.Any<CancellationToken>());
    }

    // ── JoinSearch: genuine participant must be admitted ──────────────────────

    [Fact]
    public async Task SearchCoordinationHub_JoinSearch_WhenUserIsParticipant_AddsToGroup()
    {
        // Arrange — legitimate owner or finder who has an active chat thread.
        var userId = Guid.NewGuid();
        var lostEventId = Guid.NewGuid();

        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<IsSearchParticipantQuery>(), Arg.Any<CancellationToken>())
              .Returns(Task.FromResult(Result.Success(true)));

        var (hub, groups) = BuildHub(sender, userId);

        // Act
        await hub.JoinSearch(lostEventId);

        // Assert — legitimate participant IS added to the group exactly once.
        await groups.Received(1)
                    .AddToGroupAsync(
                        "test-connection-id",
                        $"search:{lostEventId}",
                        Arg.Any<CancellationToken>());
    }

    // ── JoinSearch: unauthenticated connection must be silently denied ─────────

    [Fact]
    public async Task SearchCoordinationHub_JoinSearch_WhenUserHasNoIdentityClaim_DoesNotAddToGroup()
    {
        // Arrange — connection has no NameIdentifier claim (e.g., anonymous connection
        // that bypassed the [Authorize] filter, or token with missing sub claim).
        var lostEventId = Guid.NewGuid();

        var sender = Substitute.For<ISender>();

        var groups = Substitute.For<IGroupManager>();
        var emptyUser = new ClaimsPrincipal(new ClaimsIdentity()); // no claims
        var context = Substitute.For<HubCallerContext>();
        context.ConnectionId.Returns("anon-connection");
        context.User.Returns(emptyUser);

        var hub = new SearchCoordinationHub(sender);
        hub.Groups = groups;
        hub.Context = context;

        // Act
        await hub.JoinSearch(lostEventId);

        // Assert — no group add, and ISender was never called.
        await groups.DidNotReceive()
                    .AddToGroupAsync(
                        Arg.Any<string>(),
                        Arg.Any<string>(),
                        Arg.Any<CancellationToken>());

        await sender.DidNotReceive()
                    .Send(Arg.Any<IsSearchParticipantQuery>(), Arg.Any<CancellationToken>());
    }
}

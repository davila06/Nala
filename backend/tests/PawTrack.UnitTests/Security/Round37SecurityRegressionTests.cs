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
/// Round-37 security regression tests.
///
/// Gap: <c>SearchCoordinationHub.UpdateLocation</c> only validates that the caller
/// has an authenticated identity (<c>TryGetUserId</c>), but carries <b>no
/// <c>IsSearchParticipantQuery</c> gate</b>.
///
/// All other hub methods that write state or broadcast were gated in earlier rounds:
///   • <c>JoinSearch</c>   — participant gate added Round 28
///   • <c>ClaimZone</c>    — participant gate added Round 31
///   • <c>ClearZone</c>    — participant gate added Round 31
///   • <c>ReleaseZone</c>  — participant gate added Round 31
///
/// <c>UpdateLocation</c> is the sole remaining exception.
///
/// ── Attack vector ────────────────────────────────────────────────────────────
///   Even though the location payload uses an ephemeral <c>ConnectionId</c>
///   (not the AccountId) to preserve privacy, an unauthenticated participant can
///   still abuse this method:
///
///   1. Attacker creates a free PawTrack account.
///   2. Discovers an active search event ID from the public map.
///   3. Connects to the hub — hub upgrade is always allowed for any authenticated
///      user regardless of search participation.
///   4. Calls <c>UpdateLocation(lostEventId, fakeLat, fakeLng)</c> directly
///      without ever calling <c>JoinSearch</c>.
///   5. The broadcast fires to <c>Clients.OthersInGroup(GroupName(lostEventId))</c>.
///      Legitimate volunteers who have joined the group receive a fake GPS position
///      attributed to an ephemeral ConnectionId, poisoning their coordination map.
///
///   A single compromised account can inject an unbounded stream of false
///   coordinates into any active coordinated search.
///
/// Fix:
///   Add an <c>IsSearchParticipantQuery</c> check at the top of <c>UpdateLocation</c>,
///   identical to the pattern used in all other zone-mutation methods.
///   Non-participants receive a silent return (no error message = no info leak).
/// </summary>
public sealed class Round37SecurityRegressionTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (SearchCoordinationHub hub, ISender sender, IClientProxy groupProxy) BuildHub(
        Guid userId,
        bool isParticipant)
    {
        var sender = Substitute.For<ISender>();

        // Stub participant query
        sender.Send(Arg.Any<IsSearchParticipantQuery>(), Arg.Any<CancellationToken>())
              .Returns(Task.FromResult(Result.Success(isParticipant)));

        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            "Bearer"));

        var context = Substitute.For<HubCallerContext>();
        context.ConnectionId.Returns("test-connection-id");
        context.User.Returns(user);

        var groupProxy = Substitute.For<IClientProxy>();
        var clients = Substitute.For<IHubCallerClients>();
        clients.OthersInGroup(Arg.Any<string>()).Returns(groupProxy);

        var hub = new SearchCoordinationHub(sender);
        hub.Context = context;
        hub.Clients = clients;

        return (hub, sender, groupProxy);
    }

    // ── UpdateLocation: non-participant must be silently denied ───────────────

    [Fact]
    public async Task SearchCoordinationHub_UpdateLocation_WhenUserIsNotParticipant_DoesNotBroadcast()
    {
        // Arrange — attacker is NOT a participant in this search.
        var userId = Guid.NewGuid();
        var lostEventId = Guid.NewGuid();

        var (hub, sender, groupProxy) = BuildHub(userId, isParticipant: false);

        // Act — attacker sends valid-looking coordinates
        await hub.UpdateLocation(lostEventId, lat: 9.9, lng: -84.1);

        // Assert — the participant check must have been consulted
        await sender.Received(1)
                    .Send(Arg.Is<IsSearchParticipantQuery>(q =>
                              q.LostEventId == lostEventId && q.UserId == userId),
                          Arg.Any<CancellationToken>());

        // Assert — the broadcast must NOT have fired
        await groupProxy.DidNotReceive()
                        .SendCoreAsync(
                            Arg.Any<string>(),
                            Arg.Any<object?[]?>(),
                            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchCoordinationHub_UpdateLocation_WhenUserIsParticipant_BroadcastsLocation()
    {
        // Arrange — legitimate participant (owner or finder with chat thread).
        var userId = Guid.NewGuid();
        var lostEventId = Guid.NewGuid();

        var (hub, _, groupProxy) = BuildHub(userId, isParticipant: true);

        // Act
        await hub.UpdateLocation(lostEventId, lat: 9.934_739, lng: -84.087_502);

        // Assert — broadcast must have been sent to group
        await groupProxy.Received(1)
                        .SendCoreAsync(
                            "LocationUpdated",
                            Arg.Any<object?[]?>(),
                            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchCoordinationHub_UpdateLocation_WhenUserIsNotParticipant_DoesNotQueryCommand()
    {
        // Arrange — confirm no downstream command/query is sent (beyond the gate check itself)
        var userId = Guid.NewGuid();
        var lostEventId = Guid.NewGuid();

        var (hub, sender, _) = BuildHub(userId, isParticipant: false);

        await hub.UpdateLocation(lostEventId, lat: 9.9, lng: -84.1);

        // Only IsSearchParticipantQuery should have been sent — nothing else
        await sender.Received()
                    .Send(Arg.Any<IsSearchParticipantQuery>(), Arg.Any<CancellationToken>());

        await sender.DidNotReceive()
                    .Send(
                        Arg.Is<object>(x => x.GetType() != typeof(IsSearchParticipantQuery)),
                        Arg.Any<CancellationToken>());
    }
}

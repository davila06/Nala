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
/// Round-61 security regression tests.
///
/// Gap: <c>SearchCoordinationHub.UpdateLocation</c> has a participant gate and
/// coordinate validation but no per-connection message rate throttle:
///
///   <code>
///   public async Task UpdateLocation(Guid lostEventId, double lat, double lng)
///   {
///       if (!TryGetUserId(out var userId)) return;
///       var check = await sender.Send(new IsSearchParticipantQuery(...));
///       if (check.IsFailure || !check.Value) return;
///       if (!IsValidCoordinate(lat, lng)) return;
///       // ← no per-connection cooldown check
///       await Clients.OthersInGroup(...).SendAsync("LocationUpdated", payload);
///   }
///   </code>
///
/// A confirmed participant can open a WebSocket connection and call
/// <c>UpdateLocation</c> at the maximum frame rate their client allows — dozens or
/// hundreds of times per second — flooding every other participant's browser with
/// <c>LocationUpdated</c> events and degrading the volunteer coordination UI.
/// Because the transport is WebSocket, ASP.NET's HTTP rate limiter does not apply
/// to individual SignalR method calls.
///
/// A per-connection cooldown (e.g. one broadcast per 2 seconds per connection)
/// caps the damage: even if the user hammers the hub, only one update per cooldown
/// window is forwarded to the group.
///
/// Fix:
///   Add a <c>private static ConcurrentDictionary&lt;string, DateTimeOffset&gt;
///   _lastLocationUpdate</c> field and check the elapsed time in
///   <c>UpdateLocation</c> before broadcasting.  In <c>OnDisconnectedAsync</c>,
///   remove the entry to prevent unbounded dictionary growth.
/// </summary>
public sealed class Round61SecurityRegressionTests
{
    // ── Rapid double-call from same connection must only produce one broadcast ─

    [Fact]
    public async Task UpdateLocation_WhenCalledTwiceRapidly_BroadcastsOnlyOnce()
    {
        // Arrange — unique connection ID per test run to avoid cross-test static state
        var connectionId = $"conn-{Guid.NewGuid()}";
        var userId = Guid.NewGuid();
        var lostEventId = Guid.NewGuid();

        var (hub, groupProxy) = BuildHub(userId, connectionId, isParticipant: true);

        // Act — two consecutive calls on the same connection, no sleep between them
        // (elapsed ≈ 0 ms, well within any reasonable cooldown window)
        await hub.UpdateLocation(lostEventId, 9.93, -84.08);
        await hub.UpdateLocation(lostEventId, 9.94, -84.09);

        // Assert — only the first broadcast must reach the group
        await groupProxy.Received(1)
            .SendCoreAsync(
                "LocationUpdated",
                Arg.Any<object?[]>(),
                Arg.Any<CancellationToken>());
    }

    // ── First call must always broadcast regardless of previous silence ───────

    [Fact]
    public async Task UpdateLocation_FirstCallOnNewConnection_Broadcasts()
    {
        // Arrange — fresh connection ID guaranteed not in the throttle dictionary
        var connectionId = $"conn-{Guid.NewGuid()}";
        var userId = Guid.NewGuid();
        var lostEventId = Guid.NewGuid();

        var (hub, groupProxy) = BuildHub(userId, connectionId, isParticipant: true);

        // Act
        await hub.UpdateLocation(lostEventId, 9.93, -84.08);

        // Assert — first call must always broadcast
        await groupProxy.Received(1)
            .SendCoreAsync(
                "LocationUpdated",
                Arg.Any<object?[]>(),
                Arg.Any<CancellationToken>());
    }

    // ── Non-participant rapid calls must not broadcast at all ─────────────────
    // (regression guard: throttle must not SHORT-CIRCUIT the participant check)

    [Fact]
    public async Task UpdateLocation_NonParticipant_DoesNotBroadcast_RegardlessOfThrottle()
    {
        var connectionId = $"conn-{Guid.NewGuid()}";
        var userId = Guid.NewGuid();
        var lostEventId = Guid.NewGuid();

        var (hub, groupProxy) = BuildHub(userId, connectionId, isParticipant: false);

        await hub.UpdateLocation(lostEventId, 9.93, -84.08);
        await hub.UpdateLocation(lostEventId, 9.93, -84.08);

        // Zero broadcasts — participant gate is evaluated before throttle
        await groupProxy.DidNotReceive()
            .SendCoreAsync(
                Arg.Any<string>(),
                Arg.Any<object?[]>(),
                Arg.Any<CancellationToken>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (SearchCoordinationHub hub, IClientProxy groupProxy) BuildHub(
        Guid userId,
        string connectionId,
        bool isParticipant)
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<IsSearchParticipantQuery>(), Arg.Any<CancellationToken>())
              .Returns(Task.FromResult(Result.Success(isParticipant)));

        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "Bearer"));

        var context = Substitute.For<HubCallerContext>();
        context.ConnectionId.Returns(connectionId);
        context.User.Returns(user);

        var groupProxy = Substitute.For<IClientProxy>();
        groupProxy.SendCoreAsync(Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
                  .Returns(Task.CompletedTask);

        var clients = Substitute.For<IHubCallerClients>();
        clients.OthersInGroup(Arg.Any<string>()).Returns(groupProxy);

        var hub = new SearchCoordinationHub(sender);
        hub.Context = context;
        hub.Clients = clients;

        return (hub, groupProxy);
    }
}

using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NSubstitute;
using PawTrack.API.Hubs;
using PawTrack.Application.Common;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.LostPets.Queries.IsSearchParticipant;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;

namespace PawTrack.UnitTests.Security;

public sealed class SearchCoordinationLocationSharingTests
{
    [Fact]
    public async Task UpdateLocation_DefaultsToApproximatePrecision()
    {
        var fixture = CreateFixture();
        await fixture.Hub.StartLocationSharing(fixture.LostEventId);

        await fixture.Hub.UpdateLocation(fixture.LostEventId, 9.934739, -84.087502);

        await fixture.GroupProxy.Received().SendCoreAsync(
            "LocationUpdated",
            Arg.Is<object?[]>(args => MatchesApproximatePayload(args)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StopLocationSharing_AuditsTransitionAndStopsBroadcasts()
    {
        var fixture = CreateFixture();
        await fixture.Hub.StartLocationSharing(fixture.LostEventId, precise: true);
        await fixture.Hub.StopLocationSharing(fixture.LostEventId);
        await fixture.Hub.UpdateLocation(fixture.LostEventId, 9.934739, -84.087502);

        await fixture.Audit.Received().AddAsync(
            Arg.Is<AuditLogEntry>(entry => entry.Action == AuditAction.SearchLocationSharingStarted),
            Arg.Any<CancellationToken>());
        await fixture.Audit.Received().AddAsync(
            Arg.Is<AuditLogEntry>(entry => entry.Action == AuditAction.SearchLocationSharingStopped),
            Arg.Any<CancellationToken>());
        await fixture.GroupProxy.DidNotReceive().SendCoreAsync(
            "LocationUpdated",
            Arg.Any<object?[]>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExpiredSharing_AuditsExpirationWithoutBroadcastingCoordinates()
    {
        var fixture = CreateFixture();
        await fixture.Hub.StartLocationSharing(fixture.LostEventId);
        await fixture.Cache.RemoveAsync(fixture.ActiveSharingKey);

        await fixture.Hub.UpdateLocation(fixture.LostEventId, 9.934739, -84.087502);

        await fixture.Audit.Received().AddAsync(
            Arg.Is<AuditLogEntry>(entry => entry.Action == AuditAction.SearchLocationSharingExpired),
            Arg.Any<CancellationToken>());
        await fixture.GroupProxy.DidNotReceive().SendCoreAsync(
            "LocationUpdated",
            Arg.Any<object?[]>(),
            Arg.Any<CancellationToken>());
    }

    private static Fixture CreateFixture()
    {
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<IsSearchParticipantQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(true)));

        var cache = new MemoryDistributedCache(
            Options.Create(new MemoryDistributedCacheOptions()));
        var audit = Substitute.For<IAuditLogRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var groupProxy = Substitute.For<IClientProxy>();
        groupProxy.SendCoreAsync(
                Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var clients = Substitute.For<IHubCallerClients>();
        clients.Group(Arg.Any<string>()).Returns(groupProxy);
        clients.OthersInGroup(Arg.Any<string>()).Returns(groupProxy);

        var userId = Guid.NewGuid();
        var lostEventId = Guid.NewGuid();
        var connectionId = $"connection-{Guid.NewGuid():N}";
        var context = Substitute.For<HubCallerContext>();
        context.ConnectionId.Returns(connectionId);
        context.User.Returns(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "Bearer")));

        var hub = new SearchCoordinationHub(sender, cache, audit, unitOfWork)
        {
            Context = context,
            Clients = clients,
        };

        return new Fixture(
            hub,
            cache,
            audit,
            groupProxy,
            lostEventId,
            $"search-loc-sharing:{lostEventId:N}:{connectionId}");
    }

    private sealed record Fixture(
        SearchCoordinationHub Hub,
        IDistributedCache Cache,
        IAuditLogRepository Audit,
        IClientProxy GroupProxy,
        Guid LostEventId,
        string ActiveSharingKey);

    private static bool MatchesApproximatePayload(object?[] args) =>
        args.Length == 1 &&
        args[0] is LocationBroadcastPayload { IsPrecise: false, Lat: 9.935, Lng: -84.088 };
}

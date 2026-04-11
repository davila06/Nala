using FluentAssertions;
using PawTrack.API.Hubs;

namespace PawTrack.UnitTests.Security;

/// <summary>
/// Round-20 security regression tests.
///
/// Gap: <c>SearchCoordinationHub.UpdateLocation</c> broadcast previously included
/// <c>UserId</c> (authenticated user's internal account GUID) in the
/// <c>LocationUpdated</c> SignalR event payload sent to every other participant
/// in the same search group.
///
/// Attack vector:
///   1. Any authenticated user can join a search group for any active lost-pet event
///      (JoinSearch has no ownership check — by design, to allow volunteers).
///   2. Every time a volunteer calls UpdateLocation, all other group members
///      receive { UserId, Lat, Lng } in real time.
///   3. The account GUID is cross-referenceable with PublicPetProfileDto.OwnerId
///      and any future endpoint that accepts or returns user GUIDs.
///   4. By correlating GUID → identity → live GPS an attacker in the group can
///      build a real-time stalking map of named volunteers.
///
/// Fix: use <c>ClientId = Context.ConnectionId</c> inside a named
/// <c>LocationBroadcastPayload</c> record instead of the raw account GUID.
/// <c>ConnectionId</c> is ephemeral, session-scoped, and not cross-referenceable.
/// </summary>
public sealed class Round20SecurityRegressionTests
{
    // ── LocationBroadcastPayload structure ────────────────────────────────────

    [Fact]
    public void LocationBroadcastPayload_HasNoUserIdProperty()
    {
        // Broadcasting the internal account GUID to every search-group participant
        // enables cross-referencing with other API endpoints to identify volunteers
        // and track their real-time GPS position.
        typeof(LocationBroadcastPayload)
            .GetProperty("UserId")
            .Should().BeNull(
                "LocationBroadcastPayload must not contain UserId — the internal account GUID " +
                "exposed to all search-group members allows identity cross-referencing and " +
                "real-time stalking of volunteers via the SignalR LocationUpdated event");
    }

    [Fact]
    public void LocationBroadcastPayload_HasClientIdProperty()
    {
        // ClientId = Context.ConnectionId is ephemeral and resets on every WS session.
        // It allows the UI to distinguish moving dots on the map without revealing account identity.
        var prop = typeof(LocationBroadcastPayload)
            .GetProperty(nameof(LocationBroadcastPayload.ClientId));

        prop.Should().NotBeNull(
            "LocationBroadcastPayload must expose ClientId (SignalR ConnectionId) so the " +
            "UI can distinguish multiple volunteers on the search map");

        prop!.PropertyType.Should().Be(typeof(string),
            "ClientId must be a string — SignalR ConnectionId is a string");
    }

    [Fact]
    public void LocationBroadcastPayload_PreservesCoordinates()
    {
        typeof(LocationBroadcastPayload).GetProperty(nameof(LocationBroadcastPayload.Lat))
            .Should().NotBeNull("Lat must be preserved — it is the core purpose of the payload");

        typeof(LocationBroadcastPayload).GetProperty(nameof(LocationBroadcastPayload.Lng))
            .Should().NotBeNull("Lng must be preserved — it is the core purpose of the payload");
    }

    // ── Coordinate validation (existing guard, documented here for regression) ──

    [Fact]
    public void SearchCoordinationHub_IsValidCoordinate_RejectsNaN()
    {
        SearchCoordinationHub.IsValidCoordinate(double.NaN, 0).Should().BeFalse(
            "NaN latitude must be rejected before broadcasting");
    }

    [Fact]
    public void SearchCoordinationHub_IsValidCoordinate_RejectsInfinity()
    {
        SearchCoordinationHub.IsValidCoordinate(double.PositiveInfinity, 0).Should().BeFalse();
        SearchCoordinationHub.IsValidCoordinate(0, double.NegativeInfinity).Should().BeFalse();
    }

    [Fact]
    public void SearchCoordinationHub_IsValidCoordinate_RejectsOutOfRange()
    {
        SearchCoordinationHub.IsValidCoordinate(91.0, 0).Should().BeFalse();
        SearchCoordinationHub.IsValidCoordinate(-91.0, 0).Should().BeFalse();
        SearchCoordinationHub.IsValidCoordinate(0, 181.0).Should().BeFalse();
        SearchCoordinationHub.IsValidCoordinate(0, -181.0).Should().BeFalse();
    }

    [Fact]
    public void SearchCoordinationHub_IsValidCoordinate_AcceptsValidCrCoordinates()
    {
        // San José, Costa Rica: ~9.9281° N, 84.0907° W
        SearchCoordinationHub.IsValidCoordinate(9.9281, -84.0907).Should().BeTrue(
            "Valid Costa Rica coordinates must pass the guard");
    }
}

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PawTrack.Application.LostPets.Commands.ClaimZone;
using PawTrack.Application.LostPets.Commands.ClearZone;
using PawTrack.Application.LostPets.Commands.ReleaseZone;
using PawTrack.Application.LostPets.Queries.IsSearchParticipant;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace PawTrack.API.Hubs;

/// <summary>
/// Real-time SignalR hub for coordinated field searches (Mejora H).
/// Clients join a group per <c>lostEventId</c> and receive live zone-state broadcasts.
///
/// <para>Hub route: <c>/hubs/search-coordination</c></para>
/// <para>All methods require authentication (<see cref="AuthorizeAttribute"/>).</para>
/// </summary>
[Authorize]
public sealed class SearchCoordinationHub(ISender sender) : Hub
{
    // Per-connection location-update throttle (R61).
    // Keyed by ConnectionId — removed in OnDisconnectedAsync to prevent unbounded growth.
    private static readonly ConcurrentDictionary<string, DateTimeOffset> _lastLocationUpdate = new();
    private static readonly TimeSpan _locationThrottleInterval = TimeSpan.FromSeconds(2);

    // ── Group management ──────────────────────────────────────────────────────

    /// <summary>Joins the SignalR group for a given lost-pet event so the client receives zone broadcasts.</summary>
    public async Task JoinSearch(Guid lostEventId)
    {
        // Gate: only the event owner or a user with an active chat thread for the
        // event (i.e., an engaged finder/rescuer) may receive GPS broadcasts.
        // Active lostEventId GUIDs are publicly enumerable via GET /api/public/map,
        // so any authenticated user can discover them — the participant check is the
        // only barrier preventing arbitrary accounts from passively surveilling
        // volunteer movements across all active searches.
        if (!TryGetUserId(out var userId)) return; // no identity — silently deny

        var check = await sender.Send(
            new IsSearchParticipantQuery(lostEventId, userId));

        if (check.IsFailure || !check.Value) return; // not a participant — silently deny, no info leak

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(lostEventId));
    }

    /// <summary>Leaves the group when the user navigates away.</summary>
    public async Task LeaveSearch(Guid lostEventId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(lostEventId));
    }

    // ── Zone state transitions ────────────────────────────────────────────────

    /// <summary>Claims a free zone for the caller. Broadcasts <c>ZoneClaimed</c> to all group members on success.</summary>
    public async Task ClaimZone(Guid lostEventId, Guid zoneId)
    {
        if (!TryGetUserId(out var userId)) return;

        // Participant gate — identical to JoinSearch (Round 28).
        // Hub methods are socket-level and do NOT require prior group membership;
        // without this check any authenticated user can claim all 49 zones,
        // paralysing a coordinated field search.
        var check = await sender.Send(new IsSearchParticipantQuery(lostEventId, userId));
        if (check.IsFailure || !check.Value) return;

        var result = await sender.Send(new ClaimZoneCommand(zoneId, userId));
        if (result.IsFailure) return;

        await Clients.Group(GroupName(lostEventId))
            .SendAsync("ZoneClaimed", result.Value);
    }

    /// <summary>Marks a taken zone as fully searched. Broadcasts <c>ZoneCleared</c> to all group members on success.</summary>
    public async Task ClearZone(Guid lostEventId, Guid zoneId)
    {
        if (!TryGetUserId(out var userId)) return;

        var check = await sender.Send(new IsSearchParticipantQuery(lostEventId, userId));
        if (check.IsFailure || !check.Value) return;

        var result = await sender.Send(new ClearZoneCommand(zoneId, userId));
        if (result.IsFailure) return;

        await Clients.Group(GroupName(lostEventId))
            .SendAsync("ZoneCleared", result.Value);
    }

    /// <summary>Releases a taken zone back to Free. Broadcasts <c>ZoneReleased</c> to all group members on success.</summary>
    public async Task ReleaseZone(Guid lostEventId, Guid zoneId)
    {
        if (!TryGetUserId(out var userId)) return;

        var check = await sender.Send(new IsSearchParticipantQuery(lostEventId, userId));
        if (check.IsFailure || !check.Value) return;

        var result = await sender.Send(new ReleaseZoneCommand(zoneId, userId));
        if (result.IsFailure) return;

        await Clients.Group(GroupName(lostEventId))
            .SendAsync("ZoneReleased", result.Value);
    }

    // ── Optional GPS sharing (opt-in, not persisted) ──────────────────────────

    /// <summary>
    /// Broadcasts the caller's current GPS position to all coordinators in the same search group.
    /// Positions are <b>never persisted</b> — they are ephemeral real-time signals only.
    /// </summary>
    public async Task UpdateLocation(Guid lostEventId, double lat, double lng)
    {
        if (!TryGetUserId(out var userId)) return;

        // Participant gate — mirrors ClaimZone/ClearZone/ReleaseZone (Round 31).
        // Any authenticated user who knows a lostEventId could otherwise broadcast
        // arbitrary GPS noise to all active search volunteers.
        var check = await sender.Send(new IsSearchParticipantQuery(lostEventId, userId));
        if (check.IsFailure || !check.Value) return;

        // Reject NaN, Infinity, and out-of-range values before broadcasting.
        // A malicious or buggy client must not be able to propagate garbage coordinates
        // to all search participants.
        if (!IsValidCoordinate(lat, lng)) return;

        // Per-connection throttle: one broadcast per _locationThrottleInterval (R61).
        // WebSocket frames are not subject to ASP.NET's HTTP rate limiter, so without
        // this guard a participant can flood all other volunteers with location events
        // at the maximum WebSocket frame rate their client allows.
        var now = DateTimeOffset.UtcNow;
        var connectionId = Context.ConnectionId;
        if (_lastLocationUpdate.TryGetValue(connectionId, out var lastTime) &&
            now - lastTime < _locationThrottleInterval)
            return; // too frequent — silently drop
        _lastLocationUpdate[connectionId] = now;

        // Use ConnectionId (ephemeral, session-scoped) as the client identifier.
        // The authenticated UserId (account GUID) must NOT be broadcast to other group
        // members — it is cross-referenceable with other API endpoints and would allow
        // any participant to build an identity-linked GPS-tracking map of volunteers.
        var payload = new LocationBroadcastPayload(Context.ConnectionId, lat, lng);

        await Clients.OthersInGroup(GroupName(lostEventId))
            .SendAsync("LocationUpdated", payload);
    }

    // ── Connection lifecycle ──────────────────────────────────────────────────

    /// <summary>
    /// Removes the per-connection throttle entry when the WebSocket closes to prevent
    /// unbounded growth of <see cref="_lastLocationUpdate"/>.
    /// </summary>
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _lastLocationUpdate.TryRemove(Context.ConnectionId, out _);
        return base.OnDisconnectedAsync(exception);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool TryGetUserId(out Guid userId)
    {
        var claim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? Context.User?.FindFirstValue("sub");
        return Guid.TryParse(claim, out userId);
    }

    private static string GroupName(Guid lostEventId) => $"search:{lostEventId}";

    /// <summary>
    /// Returns true only for finite, in-range GPS coordinates.
    /// Rejects NaN, ±Infinity, and values outside the valid WGS-84 bounds.
    /// </summary>
    public static bool IsValidCoordinate(double lat, double lng) =>
        double.IsFinite(lat) && double.IsFinite(lng) &&
        lat is >= -90.0 and <= 90.0 &&
        lng is >= -180.0 and <= 180.0;
}

/// <summary>
/// Payload broadcast to all other search-group members on <c>LocationUpdated</c>.
/// <para>
/// <b>Privacy:</b> <c>ClientId</c> is the SignalR <c>ConnectionId</c>, NOT the
/// authenticated user's account GUID.  <c>ConnectionId</c> is ephemeral (resets
/// on every WebSocket session) and is not cross-referenceable with any other API
/// endpoint, preventing identity-linked GPS tracking of search volunteers.
/// </para>
/// </summary>
/// <param name="ClientId">Ephemeral SignalR ConnectionId — resets on reconnect.</param>
/// <param name="Lat">WGS-84 latitude, pre-validated to [−90, 90].</param>
/// <param name="Lng">WGS-84 longitude, pre-validated to [−180, 180].</param>
public sealed record LocationBroadcastPayload(string ClientId, double Lat, double Lng);

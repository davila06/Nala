using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using PawTrack.Application.LostPets.Commands.ClaimZone;
using PawTrack.Application.LostPets.Commands.ClearZone;
using PawTrack.Application.LostPets.Commands.ReleaseZone;
using PawTrack.Application.LostPets.Queries.IsSearchParticipant;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json;

namespace PawTrack.API.Hubs;

/// <summary>
/// Real-time SignalR hub for coordinated field searches (Mejora H).
/// Clients join a group per <c>lostEventId</c> and receive live zone-state broadcasts.
///
/// <para>Hub route: <c>/hubs/search-coordination</c></para>
/// <para>All methods require authentication (<see cref="AuthorizeAttribute"/>).</para>
/// </summary>
[Authorize]
public sealed class SearchCoordinationHub(
    ISender sender,
    IDistributedCache cache,
    IAuditLogRepository? auditLog = null,
    IUnitOfWork? unitOfWork = null,
    ISearchLocationSharingSessionRepository? sessionRepository = null) : Hub
{
    // Per-connection location-update throttle (R61), backed by IDistributedCache (Redis)
    // so it's honored across all Container App instances, not just the one holding the
    // WebSocket connection. TTL expiry replaces the old manual OnDisconnectedAsync cleanup.
    private static readonly TimeSpan _locationThrottleInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan _locationSharingLifetime = TimeSpan.FromMinutes(30);
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _recipientLocks = new();

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
        await using (await AcquireRecipientLock(lostEventId))
        {
            await AddRecipient(lostEventId, Context.ConnectionId);
            await PublishRecipientState(lostEventId);
        }
    }

    /// <summary>Leaves the group when the user navigates away.</summary>
    public async Task LeaveSearch(Guid lostEventId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(lostEventId));
        await using (await AcquireRecipientLock(lostEventId))
        {
            await RemoveRecipient(lostEventId, Context.ConnectionId);
            await PublishRecipientState(lostEventId);
        }
    }

    /// <summary>Starts an explicit, expiring location-sharing session.</summary>
    public async Task StartLocationSharing(Guid lostEventId, bool precise = false)
    {
        if (!TryGetUserId(out var userId) || !await IsParticipant(lostEventId, userId)) return;

        var expiresAt = DateTimeOffset.UtcNow.Add(_locationSharingLifetime);
        var session = new SharingSession(userId, precise ? "precise" : "approximate", expiresAt);
        var key = SharingKey(lostEventId, Context.ConnectionId);
        await cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(session),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _locationSharingLifetime });
        await cache.SetStringAsync(
            SessionMarkerKey(lostEventId, Context.ConnectionId),
            JsonSerializer.Serialize(session),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _locationSharingLifetime.Add(TimeSpan.FromMinutes(1)) });
        if (sessionRepository is not null && unitOfWork is not null)
        {
            await sessionRepository.AddAsync(
                PawTrack.Domain.SearchCoordination.SearchLocationSharingSession.Start(
                    lostEventId,
                    userId,
                    Context.ConnectionId,
                    precise,
                    DateTimeOffset.UtcNow,
                    _locationSharingLifetime),
                Context.ConnectionAborted);
            await unitOfWork.SaveChangesAsync(Context.ConnectionAborted);
        }
        await WriteSharingAudit(userId, lostEventId, AuditAction.SearchLocationSharingStarted,
            precise ? "precise" : "approximate");
        await Clients.Group(GroupName(lostEventId)).SendAsync(
            "LocationSharingStateChanged",
            new LocationSharingState(Context.ConnectionId, true, precise, expiresAt, await GetRecipientCount(lostEventId)));
    }

    /// <summary>Immediately stops this connection's location-sharing session.</summary>
    public async Task StopLocationSharing(Guid lostEventId)
    {
        if (!TryGetUserId(out var userId)) return;
        await cache.RemoveAsync(SharingKey(lostEventId, Context.ConnectionId));
        await cache.RemoveAsync(SessionMarkerKey(lostEventId, Context.ConnectionId));
        if (sessionRepository is not null && unitOfWork is not null)
        {
            var session = await sessionRepository.GetActiveAsync(
                lostEventId, Context.ConnectionId, Context.ConnectionAborted);
            if (session is not null)
            {
                session.Stop(DateTimeOffset.UtcNow);
                sessionRepository.Update(session);
                await unitOfWork.SaveChangesAsync(Context.ConnectionAborted);
            }
        }
        await WriteSharingAudit(userId, lostEventId, AuditAction.SearchLocationSharingStopped, null);
        await Clients.Group(GroupName(lostEventId)).SendAsync(
            "LocationSharingStateChanged",
            new LocationSharingState(Context.ConnectionId, false, false, null, await GetRecipientCount(lostEventId)));
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

        var session = await GetActiveSession(lostEventId, Context.ConnectionId);
        if (session is null) return; // explicit consent is required

        // Reject NaN, Infinity, and out-of-range values before broadcasting.
        // A malicious or buggy client must not be able to propagate garbage coordinates
        // to all search participants.
        if (!IsValidCoordinate(lat, lng)) return;

        // Per-connection throttle: one broadcast per _locationThrottleInterval (R61).
        // WebSocket frames are not subject to ASP.NET's HTTP rate limiter, so without
        // this guard a participant can flood all other volunteers with location events
        // at the maximum WebSocket frame rate their client allows.
        var throttleKey = $"search-loc-throttle:{Context.ConnectionId}";
        if (await cache.GetStringAsync(throttleKey) is not null)
            return; // too frequent — silently drop
        await cache.SetStringAsync(throttleKey, "1",
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _locationThrottleInterval });

        // Use ConnectionId (ephemeral, session-scoped) as the client identifier.
        // The authenticated UserId (account GUID) must NOT be broadcast to other group
        // members — it is cross-referenceable with other API endpoints and would allow
        // any participant to build an identity-linked GPS-tracking map of volunteers.
        var precise = session.Mode == "precise";
        var payload = new LocationBroadcastPayload(
            Context.ConnectionId,
            precise ? lat : RoundCoordinate(lat),
            precise ? lng : RoundCoordinate(lng),
            precise,
            session.ExpiresAt);

        await Clients.OthersInGroup(GroupName(lostEventId))
            .SendAsync("LocationUpdated", payload);
    }

    // ── Connection lifecycle ──────────────────────────────────────────────────

    // Throttle entries expire via TTL in IDistributedCache — no manual cleanup needed.

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool TryGetUserId(out Guid userId)
    {
        var claim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? Context.User?.FindFirstValue("sub");
        return Guid.TryParse(claim, out userId);
    }

    private static string GroupName(Guid lostEventId) => $"search:{lostEventId}";

    private static string SharingKey(Guid lostEventId, string connectionId) =>
        $"search-loc-sharing:{lostEventId:N}:{connectionId}";

    private static string SessionMarkerKey(Guid lostEventId, string connectionId) =>
        $"search-loc-sharing-marker:{lostEventId:N}:{connectionId}";

    private static string RecipientsKey(Guid lostEventId) =>
        $"search-loc-recipients:{lostEventId:N}";

    private async Task<SharingSession?> GetActiveSession(Guid lostEventId, string connectionId)
    {
        var active = await cache.GetStringAsync(SharingKey(lostEventId, connectionId));
        if (active is not null)
            return JsonSerializer.Deserialize<SharingSession>(active);

        var markerJson = await cache.GetStringAsync(SessionMarkerKey(lostEventId, connectionId));
        if (markerJson is null) return null;

        var expired = JsonSerializer.Deserialize<SharingSession>(markerJson);
        if (expired is not null)
        {
            await cache.RemoveAsync(SessionMarkerKey(lostEventId, connectionId));
            await WriteSharingAudit(expired.UserId, lostEventId, AuditAction.SearchLocationSharingExpired, null);
            await PublishRecipientState(lostEventId);
        }

        return null;
    }

    private async Task AddRecipient(Guid lostEventId, string connectionId)
    {
        var recipients = await GetRecipients(lostEventId);
        if (!recipients.Contains(connectionId, StringComparer.Ordinal))
            recipients.Add(connectionId);
        await SaveRecipients(lostEventId, recipients);
    }

    private static async ValueTask<IAsyncDisposable> AcquireRecipientLock(Guid lostEventId)
    {
        var gate = _recipientLocks.GetOrAdd(lostEventId, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        return new SemaphoreReleaser(gate);
    }

    private sealed class SemaphoreReleaser(SemaphoreSlim semaphore) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            semaphore.Release();
            return ValueTask.CompletedTask;
        }
    }

    private async Task RemoveRecipient(Guid lostEventId, string connectionId)
    {
        var recipients = await GetRecipients(lostEventId);
        recipients.RemoveAll(id => string.Equals(id, connectionId, StringComparison.Ordinal));
        await SaveRecipients(lostEventId, recipients);
    }

    private async Task<List<string>> GetRecipients(Guid lostEventId)
    {
        var json = await cache.GetStringAsync(RecipientsKey(lostEventId));
        return json is null ? [] : JsonSerializer.Deserialize<List<string>>(json) ?? [];
    }

    private async Task SaveRecipients(Guid lostEventId, List<string> recipients) =>
        await cache.SetStringAsync(
            RecipientsKey(lostEventId),
            JsonSerializer.Serialize(recipients),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _locationSharingLifetime });

    private async Task<int> GetRecipientCount(Guid lostEventId) =>
        Math.Max(0, (await GetRecipients(lostEventId)).Count - 1);

    private async Task PublishRecipientState(Guid lostEventId)
    {
        if (Clients is null) return;
        var recipients = await GetRecipients(lostEventId);
        await Clients.Group(GroupName(lostEventId)).SendAsync(
            "LocationSharingRecipientsChanged",
            new LocationSharingRecipientsState(Math.Max(0, recipients.Count - 1), recipients));
    }

    private async Task<bool> IsParticipant(Guid lostEventId, Guid userId)
    {
        var check = await sender.Send(new IsSearchParticipantQuery(lostEventId, userId));
        return check.IsSuccess && check.Value;
    }

    private async Task WriteSharingAudit(
        Guid userId,
        Guid lostEventId,
        AuditAction action,
        string? details)
    {
        if (auditLog is null || unitOfWork is null) return;
        await auditLog.AddAsync(AuditLogEntry.Create(
            userId, action, "SearchLocationSharing", lostEventId.ToString(), details));
        await unitOfWork.SaveChangesAsync(Context.ConnectionAborted);
    }

    private static double RoundCoordinate(double value) => Math.Round(value, 3, MidpointRounding.ToEven);

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
public sealed record LocationBroadcastPayload(
    string ClientId,
    double Lat,
    double Lng,
    bool IsPrecise,
    DateTimeOffset ExpiresAt);

public sealed record LocationSharingState(
    string ClientId,
    bool IsSharing,
    bool IsPrecise,
    DateTimeOffset? ExpiresAt,
    int RecipientCount);

public sealed record LocationSharingRecipientsState(
    int RecipientCount,
    IReadOnlyList<string> ClientIds);

internal sealed record SharingSession(Guid UserId, string Mode, DateTimeOffset ExpiresAt);

using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.LostPets.DTOs;
using PawTrack.Application.Sightings.DTOs;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Common;
using PawTrack.Domain.Notifications;

namespace PawTrack.Application.LostPets.Queries.GetCaseRoom;

// ── Response DTO ──────────────────────────────────────────────────────────────

/// <summary>
/// Anonymised summary of a geofenced alert sent to a user nearby the lost-pet location.
/// No PII is exposed — only the notification metadata and a recipient count.
/// </summary>
public sealed record NearbyAlertSummary(
    string NotificationId,
    DateTimeOffset SentAt,
    string Title);

/// <summary>Nearest featured clinic shown as sponsor in the Case Room.</summary>
public sealed record SponsoredClinicDto(
    Guid Id,
    string Name,
    string Address,
    string? LogoUrl,
    string ContactEmail,
    string? PhoneNumber,
    string? Website);

/// <summary>
/// Full aggregate payload for the Incident Room (Case Command Centre).
/// One network round-trip: event + sightings + notifications in a single query.
/// </summary>
public sealed record CaseRoomDto(
    LostPetEventDto Event,
    IReadOnlyList<SightingDto> Sightings,
    IReadOnlyList<NearbyAlertSummary> NearbyAlerts,
    int TotalNearbyAlertsDispatched,
    SponsoredClinicDto? SponsoredClinic,
    DateTimeOffset GeneratedAt);

// ── Query ─────────────────────────────────────────────────────────────────────

public sealed record GetCaseRoomQuery(
    Guid LostPetEventId,
    Guid RequestingUserId) : IRequest<Result<CaseRoomDto>>;

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class GetCaseRoomQueryHandler(
    ILostPetRepository lostPetRepository,
    IPetRepository petRepository,
    ISightingRepository sightingRepository,
    INotificationRepository notificationRepository,
    ISubscriptionService subscriptionService,
    IClinicRepository clinicRepository,
    ISightingPriorityScorer sightingPriorityScorer)
    : IRequestHandler<GetCaseRoomQuery, Result<CaseRoomDto>>
{
    public async Task<Result<CaseRoomDto>> Handle(
        GetCaseRoomQuery request,
        CancellationToken cancellationToken)
    {
        // ── 0. Verify Plus subscription ───────────────────────────────────────
        var isPlus = await subscriptionService.IsAtLeastPlusAsync(request.RequestingUserId, cancellationToken);
        if (!isPlus)
            return Result.Failure<CaseRoomDto>("La sala de caso requiere el plan Plus. Actualiza tu plan para acceder.");

        // ── 1. Load the event and verify ownership ────────────────────────────
        var lostEvent = await lostPetRepository.GetByIdAsync(
            request.LostPetEventId, cancellationToken);

        if (lostEvent is null)
            return Result.Failure<CaseRoomDto>("Lost pet report not found.");

        if (lostEvent.OwnerId != request.RequestingUserId)
            return Result.Failure<CaseRoomDto>("Access denied.");

        // ── 2. Fan-out: load pet, sightings, and nearby-alert notifications
        //       in parallel — independent queries, no sequential waterfall.
        var (pet, sightings, nearbyAlertNotifications) = await (
            petRepository.GetByIdAsync(lostEvent.PetId, cancellationToken),
            sightingRepository.GetByLostEventIdAsync(
                request.LostPetEventId, cancellationToken),
            notificationRepository.GetByLostEventIdAsync(
                request.LostPetEventId, cancellationToken)
        ).WhenAll();

        if (pet is null)
            return Result.Failure<CaseRoomDto>("Pet not found.");

        // ── 3. Score and rank sightings ───────────────────────────────────────
        var scoredSightings = sightings
            .Select(s =>
            {
                var priority = sightingPriorityScorer.Score(pet, lostEvent, s);
                return new { Dto = SightingDto.FromDomain(s, priority), Score = priority.Score };
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Dto.SightedAt)
            .Select(x => x.Dto)
            .ToList()
            .AsReadOnly();

        // ── 4. Build anonymised nearby-alert summaries (no user IDs in response)
        var alertSummaries = nearbyAlertNotifications
            .Where(n => n.Type == NotificationType.LostPetAlert)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NearbyAlertSummary(
                n.Id.ToString(),
                n.CreatedAt,
                n.Title))
            .ToList()
            .AsReadOnly();

        return Result.Success(new CaseRoomDto(
            LostPetEventDto.FromDomain(lostEvent),
            scoredSightings,
            alertSummaries,
            alertSummaries.Count,
            await ResolveSponsoredClinicAsync(lostEvent, cancellationToken),
            DateTimeOffset.UtcNow));
    }

    private async Task<SponsoredClinicDto?> ResolveSponsoredClinicAsync(
        PawTrack.Domain.LostPets.LostPetEvent lostEvent,
        CancellationToken cancellationToken)
    {
        if (!lostEvent.LastSeenLat.HasValue || !lostEvent.LastSeenLng.HasValue)
            return null;

        var nearby = await clinicRepository.GetFeaturedNearAsync(
            (double)lostEvent.LastSeenLat.Value,
            (double)lostEvent.LastSeenLng.Value,
            radiusKm: 15,
            cancellationToken);

        var sponsor = nearby.FirstOrDefault();
        if (sponsor is null) return null;

        return new SponsoredClinicDto(
            sponsor.Id,
            sponsor.Name,
            sponsor.Address,
            sponsor.LogoUrl,
            sponsor.ContactEmail,
            sponsor.PhoneNumber,
            sponsor.Website);
    }
}

// ── Tuple extension (avoids ValueTuple ambiguity with Task.WhenAll) ───────────

file static class TupleTaskExtensions
{
    /// <summary>
    /// Awaits three independent tasks in parallel and deconstructs the results.
    /// Equivalent to <c>await Task.WhenAll(t1, t2, t3)</c> with typed return values.
    /// </summary>
    public static async Task<(T1, T2, T3)> WhenAll<T1, T2, T3>(
        this (Task<T1> t1, Task<T2> t2, Task<T3> t3) tasks)
    {
        await Task.WhenAll(tasks.t1, tasks.t2, tasks.t3).ConfigureAwait(false);
        return (tasks.t1.Result, tasks.t2.Result, tasks.t3.Result);
    }
}

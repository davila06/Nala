using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Clinics.Queries.GetNearbyActiveAlerts;

public sealed record NearbyAlertDto(
    Guid LostPetEventId,
    string PetName,
    string? PetSpecies,
    double? LastSeenLat,
    double? LastSeenLng,
    DateTimeOffset ReportedAt,
    string? RecentPhotoUrl);

public sealed record GetNearbyActiveAlertsQuery(
    Guid ClinicId,
    Guid RequestingUserId,
    double RadiusKm = 15) : IRequest<Result<IReadOnlyList<NearbyAlertDto>>>;

public sealed class GetNearbyActiveAlertsQueryHandler(
    IClinicRepository clinicRepository,
    ILostPetRepository lostPetRepository,
    IPetRepository petRepository,
    ISubscriptionRepository subscriptionRepository)
    : IRequestHandler<GetNearbyActiveAlertsQuery, Result<IReadOnlyList<NearbyAlertDto>>>
{
    public async Task<Result<IReadOnlyList<NearbyAlertDto>>> Handle(
        GetNearbyActiveAlertsQuery request, CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || clinic.UserId != request.RequestingUserId)
            return Result.Failure<IReadOnlyList<NearbyAlertDto>>("Access denied.");

        var sub = await subscriptionRepository.GetActiveForClinicAsync(request.ClinicId, cancellationToken);
        if (sub is null || sub.Tier < SubscriptionTier.ClinicPartner)
            return Result.Failure<IReadOnlyList<NearbyAlertDto>>("Las alertas cercanas requieren el plan Clínica Partner.");

        double latDelta = request.RadiusKm / 111.0;
        double lngDelta = request.RadiusKm / (111.0 * Math.Cos((double)clinic.Lat * Math.PI / 180.0));

        var events = await lostPetRepository.GetActiveLostPetsInBBoxAsync(
            north: (double)clinic.Lat + latDelta,
            south: (double)clinic.Lat - latDelta,
            east: (double)clinic.Lng + lngDelta,
            west: (double)clinic.Lng - lngDelta,
            cancellationToken);

        var pets = await Task.WhenAll(
            events.Select(e => petRepository.GetByIdAsync(e.PetId, cancellationToken)));

        var dtos = events
            .Select((e, i) => new NearbyAlertDto(
                e.Id,
                pets[i]?.Name ?? "Mascota",
                pets[i]?.Species.ToString(),
                e.LastSeenLat.HasValue ? (double)e.LastSeenLat.Value : null,
                e.LastSeenLng.HasValue ? (double)e.LastSeenLng.Value : null,
                e.ReportedAt,
                e.RecentPhotoUrl))
            .OrderByDescending(d => d.ReportedAt)
            .ToList()
            .AsReadOnly();

        return Result.Success<IReadOnlyList<NearbyAlertDto>>(dtos);
    }
}

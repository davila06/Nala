using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.LostPets;
using PawTrack.Domain.Pets;
using PawTrack.Domain.Sightings;

namespace PawTrack.Application.Sightings.Scoring;

public sealed class SightingPriorityScorer : ISightingPriorityScorer
{
    public SightingPriority Score(
        Pet pet,
        LostPetEvent? lostPetEvent,
        Sighting sighting,
        DateTimeOffset? referenceTime = null)
    {
        ArgumentNullException.ThrowIfNull(pet);
        ArgumentNullException.ThrowIfNull(sighting);

        var now = referenceTime ?? DateTimeOffset.UtcNow;

        var score = DistanceScore(lostPetEvent, sighting)
            + RecencyScore(sighting, now)
            + PhotoEvidenceScore(sighting)
            + ActiveReportLinkScore(lostPetEvent, sighting)
            + ContextScore(sighting);

        score = Math.Clamp(score, 0, 100);
        var badge = ResolveBadge(score);

        return new SightingPriority(score, badge, ResolveRecommendation(badge));
    }

    private static int DistanceScore(LostPetEvent? lostPetEvent, Sighting sighting)
    {
        if (lostPetEvent is null)
            return 0;

        if (lostPetEvent.LastSeenLat is not double lastSeenLat || lostPetEvent.LastSeenLng is not double lastSeenLng)
            return 20;

        var distanceInMeters = GetDistanceInMeters(lastSeenLat, lastSeenLng, sighting.Lat, sighting.Lng);

        return distanceInMeters switch
        {
            <= 500 => 35,
            <= 1500 => 28,
            <= 5000 => 18,
            <= 10000 => 8,
            _ => 0,
        };
    }

    private static int RecencyScore(Sighting sighting, DateTimeOffset now)
    {
        var hoursElapsed = Math.Max(0, (now - sighting.SightedAt).TotalHours);

        return hoursElapsed switch
        {
            <= 1 => 35,
            <= 6 => 28,
            <= 24 => 18,
            <= 72 => 8,
            _ => 0,
        };
    }

    private static int PhotoEvidenceScore(Sighting sighting)
        => string.IsNullOrWhiteSpace(sighting.PhotoUrl) ? 0 : 15;

    private static int ActiveReportLinkScore(LostPetEvent? lostPetEvent, Sighting sighting)
        => lostPetEvent is not null && sighting.LostPetEventId == lostPetEvent.Id ? 10 : 0;

    private static int ContextScore(Sighting sighting)
        => string.IsNullOrWhiteSpace(sighting.Note) ? 0 : 5;

    private static SightingPriorityBadge ResolveBadge(int score)
        => score switch
        {
            >= 80 => SightingPriorityBadge.Urgent,
            >= 50 => SightingPriorityBadge.Validate,
            _ => SightingPriorityBadge.Observe,
        };

    private static string ResolveRecommendation(SightingPriorityBadge badge)
        => badge switch
        {
            SightingPriorityBadge.Urgent => "Contacta al posible testigo y revisa la zona en los próximos 15 minutos.",
            SightingPriorityBadge.Validate => "Valida la pista con una llamada o visita rápida antes de ampliar la búsqueda.",
            _ => "Mantén la pista en observación y compárala con nuevos avistamientos antes de movilizar recursos.",
        };

    private static double GetDistanceInMeters(double startLat, double startLng, double endLat, double endLng)
    {
        const double EarthRadiusInMeters = 6371000;

        var startLatRadians = DegreesToRadians(startLat);
        var endLatRadians = DegreesToRadians(endLat);
        var deltaLat = DegreesToRadians(endLat - startLat);
        var deltaLng = DegreesToRadians(endLng - startLng);

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2)
            + Math.Cos(startLatRadians) * Math.Cos(endLatRadians)
            * Math.Sin(deltaLng / 2) * Math.Sin(deltaLng / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusInMeters * c;
    }

    private static double DegreesToRadians(double degrees)
        => degrees * (Math.PI / 180d);
}
using MediatR;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Sightings.VisualMatch;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Sightings.VisualMatch;

/// <summary>
/// Public (unauthenticated) photo matching query. Returns top 5 candidates only.
/// Intentionally skips quota tracking — rate limiting enforced at the API layer.
/// </summary>
public sealed record PublicQuickMatchQuery(
    Stream PhotoStream,
    string PhotoContentType,
    double? Lat,
    double? Lng)
    : IRequest<Result<IReadOnlyList<VisualMatchDto>>>;

public sealed class PublicQuickMatchQueryHandler(
    IImageEmbeddingService embeddingService,
    IVisualMatchRepository visualMatchRepository,
    IUnitOfWork unitOfWork,
    VisualMatchSettings settings,
    ILogger<PublicQuickMatchQueryHandler> logger)
    : IRequestHandler<PublicQuickMatchQuery, Result<IReadOnlyList<VisualMatchDto>>>
{
    private const int MaxResults = 5;
    private const float MinThreshold = 0.40f;
    private const float CosineWeight = 0.70f;
    private const float GeoWeight = 0.30f;

    public async Task<Result<IReadOnlyList<VisualMatchDto>>> Handle(
        PublicQuickMatchQuery request, CancellationToken ct)
    {
        var probeVector = await embeddingService.VectorizeStreamAsync(
            request.PhotoStream, request.PhotoContentType, ct);

        if (probeVector is null)
            return Result.Failure<IReadOnlyList<VisualMatchDto>>(
                "No se pudo analizar la imagen. Usa una foto clara con buena iluminación.");

        var profiles = await visualMatchRepository.GetActiveLostPetProfilesAsync(ct);
        if (profiles.Count == 0)
            return Result.Success<IReadOnlyList<VisualMatchDto>>([]);

        var petIds = profiles.Select(p => p.PetId);
        var embedded = await visualMatchRepository.GetEmbeddingsByPetIdsAsync(petIds, ct);
        var hasNew = false;

        var scored = new List<(ActiveLostPetProfile Profile, float Score, float? DistanceKm)>(profiles.Count);

        foreach (var profile in profiles)
        {
            if (profile.PhotoUrl is null) continue;

            var urlHash = MatchSightingPhotoQueryHandler.ComputeUrlHash(profile.PhotoUrl);
            float[] petVector;

            if (embedded.TryGetValue(profile.PetId, out var cached) && cached.PhotoUrlHash == urlHash)
            {
                try { petVector = cached.DeserializeVector(); }
                catch { continue; }
            }
            else
            {
                var gen = await embeddingService.VectorizeUrlAsync(profile.PhotoUrl, ct);
                if (gen is null) continue;
                var rec = PawTrack.Domain.Pets.PetPhotoEmbedding.Create(
                    profile.PetId, System.Text.Json.JsonSerializer.Serialize(gen), urlHash);
                await visualMatchRepository.UpsertEmbeddingAsync(rec, ct);
                hasNew = true;
                petVector = gen;
            }

            var cosine = VectorMath.CosineSimilarity(probeVector, petVector);
            if (cosine < MinThreshold) continue;

            float? distKm = null;
            if (request.Lat.HasValue && request.Lng.HasValue
                && profile.LastSeenLat.HasValue && profile.LastSeenLng.HasValue)
                distKm = (float)VectorMath.HaversineKm(
                    request.Lat.Value, request.Lng.Value,
                    (double)profile.LastSeenLat.Value, (double)profile.LastSeenLng.Value);

            var geo = VectorMath.GeoProximityScore(
                request.Lat, request.Lng, profile.LastSeenLat, profile.LastSeenLng);

            scored.Add((profile, cosine * CosineWeight + geo * GeoWeight, distKm));
        }

        if (hasNew) await unitOfWork.SaveChangesAsync(ct);

        var results = scored
            .OrderByDescending(x => x.Score)
            .Take(MaxResults)
            .Select(x => new VisualMatchDto(
                x.Profile.PetId.ToString(),
                x.Profile.LostEventId.ToString(),
                x.Profile.PetName,
                x.Profile.Species,
                x.Profile.PhotoUrl,
                x.Score,
                x.DistanceKm,
                $"{settings.BaseUrl}/p/{x.Profile.PetId}"))
            .ToList()
            .AsReadOnly();

        logger.LogInformation(
            "PublicQuickMatch: probe matched {Count}/{Total} profiles (top {Max} returned)",
            scored.Count, profiles.Count, MaxResults);

        return Result.Success<IReadOnlyList<VisualMatchDto>>(results);
    }
}

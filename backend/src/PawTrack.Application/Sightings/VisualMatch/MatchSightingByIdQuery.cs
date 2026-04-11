using MediatR;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Pets;
using System.Text.Json;

namespace PawTrack.Application.Sightings.VisualMatch;

/// <summary>
/// Visual-match variant that uses the photo URL already stored on a <see cref="Domain.Sightings.Sighting"/>
/// (uploaded during report submission) as the probe image, bypassing re-upload.
/// <para>
/// Returns the same DTO list as <see cref="MatchSightingPhotoQuery"/> so the same
/// frontend component can render both flows.
/// </para>
/// </summary>
/// <param name="SightingId">ID of the sighting whose stored <c>PhotoUrl</c> should be probed.</param>
/// <param name="Lat">Optional: override probe location (defaults to <c>Sighting.Lat / Lng</c>).</param>
/// <param name="Lng">Optional: override probe location.</param>
public sealed record MatchSightingByIdQuery(
    Guid    SightingId,
    double? Lat = null,
    double? Lng = null)
    : IRequest<Result<IReadOnlyList<VisualMatchDto>>>;

public sealed class MatchSightingByIdQueryHandler(
    ISightingRepository      sightingRepository,
    IImageEmbeddingService   embeddingService,
    IVisualMatchRepository   visualMatchRepository,
    IUnitOfWork              unitOfWork,
    VisualMatchSettings      settings,
    ILogger<MatchSightingByIdQueryHandler> logger)
    : IRequestHandler<MatchSightingByIdQuery, Result<IReadOnlyList<VisualMatchDto>>>
{
    private const int   TopK                   = 35;
    private const float MinSimilarityThreshold = 0.40f;
    private const float CosineWeight           = 0.70f;
    private const float GeoWeight              = 0.30f;

    public async Task<Result<IReadOnlyList<VisualMatchDto>>> Handle(
        MatchSightingByIdQuery request,
        CancellationToken      cancellationToken)
    {
        // ── 1. Resolve sighting ───────────────────────────────────────────────
        var sighting = await sightingRepository.GetByIdAsync(request.SightingId, cancellationToken);
        if (sighting is null)
            return Result.Failure<IReadOnlyList<VisualMatchDto>>("Sighting not found.");

        if (string.IsNullOrWhiteSpace(sighting.PhotoUrl))
            return Result.Failure<IReadOnlyList<VisualMatchDto>>(
                "This sighting has no photo — upload a photo to use visual matching.");

        // ── 2. Vectorise the stored sighting photo via its URL ────────────────
        var probeVector = await embeddingService.VectorizeUrlAsync(sighting.PhotoUrl, cancellationToken);
        if (probeVector is null)
            return Result.Failure<IReadOnlyList<VisualMatchDto>>(
                "No se pudo analizar la foto. Intenta de nuevo en unos momentos.");

        // ── 3. Use sighting coordinates as default probe location ─────────────
        var probeLat = request.Lat ?? sighting.Lat;
        var probeLng = request.Lng ?? sighting.Lng;

        // ── 4. Load active lost-pet profiles + batch embeddings ───────────────
        var profiles = await visualMatchRepository.GetActiveLostPetProfilesAsync(cancellationToken);
        if (profiles.Count == 0)
            return Result.Success<IReadOnlyList<VisualMatchDto>>([]);

        var petIds   = profiles.Select(p => p.PetId);
        var embedded = await visualMatchRepository.GetEmbeddingsByPetIdsAsync(petIds, cancellationToken);

        // ── 5. Score ──────────────────────────────────────────────────────────
        var baseUrl          = settings.BaseUrl;
        var hasNewEmbeddings = false;

        var scored = new List<(ActiveLostPetProfile Profile, float Score, float? DistanceKm)>(profiles.Count);

        foreach (var profile in profiles)
        {
            if (profile.PhotoUrl is null) continue;

            var photoUrlHash = MatchSightingPhotoQueryHandler.ComputeUrlHash(profile.PhotoUrl);
            float[] petVector;

            if (embedded.TryGetValue(profile.PetId, out var cached)
                && cached.PhotoUrlHash == photoUrlHash)
            {
                try { petVector = cached.DeserializeVector(); }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Corrupt embedding for pet {PetId}; regenerating", profile.PetId);
                    var regenerated = await RegenerateEmbeddingAsync(
                        profile, photoUrlHash, cancellationToken);
                    if (regenerated.Persisted)
                        hasNewEmbeddings = true;
                    petVector = regenerated.Vector;
                    if (petVector is null) continue;
                }
            }
            else
            {
                var regenerated = await RegenerateEmbeddingAsync(
                    profile, photoUrlHash, cancellationToken);
                if (regenerated.Persisted)
                    hasNewEmbeddings = true;
                petVector = regenerated.Vector;
                if (petVector is null) continue;
            }

            var cosine = VectorMath.CosineSimilarity(probeVector, petVector);
            if (cosine < MinSimilarityThreshold) continue;

            float? distKm = null;
            if (profile.LastSeenLat.HasValue && profile.LastSeenLng.HasValue)
            {
                distKm = (float)VectorMath.HaversineKm(
                    probeLat, probeLng,
                    profile.LastSeenLat.Value, profile.LastSeenLng.Value);
            }

            var geoScore = VectorMath.GeoProximityScore(
                probeLat, probeLng, profile.LastSeenLat, profile.LastSeenLng);

            scored.Add((profile, cosine * CosineWeight + geoScore * GeoWeight, distKm));
        }

        if (hasNewEmbeddings) await unitOfWork.SaveChangesAsync(cancellationToken);

        // ── 6. Return top-35 ──────────────────────────────────────────────────
        var results = scored
            .OrderByDescending(x => x.Score)
            .Take(TopK)
            .Select(x => new VisualMatchDto(
                x.Profile.PetId.ToString(),
                x.Profile.LostEventId.ToString(),
                x.Profile.PetName,
                x.Profile.Species,
                x.Profile.PhotoUrl,
                x.Score,
                x.DistanceKm,
                $"{baseUrl}/p/{x.Profile.PetId}"))
            .ToList()
            .AsReadOnly();

        return Result.Success<IReadOnlyList<VisualMatchDto>>(results);
    }

    private async Task<(float[]? Vector, bool Persisted)> RegenerateEmbeddingAsync(
        ActiveLostPetProfile profile,
        string               photoUrlHash,
        CancellationToken    ct)
    {
        var generated = await embeddingService.VectorizeUrlAsync(profile.PhotoUrl!, ct);
        if (generated is null)
        {
            logger.LogDebug("Azure Vision unavailable for pet {PetId}; skipping", profile.PetId);
            return (null, false);
        }

        var json      = JsonSerializer.Serialize(generated);
        var newRecord = PetPhotoEmbedding.Create(profile.PetId, json, photoUrlHash);
        await visualMatchRepository.UpsertEmbeddingAsync(newRecord, ct);
        return (generated, true);
    }
}

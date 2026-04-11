using MediatR;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Pets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PawTrack.Application.Sightings.VisualMatch;

// ── DTOs ──────────────────────────────────────────────────────────────────────

/// <summary>A single visual-match candidate returned to the caller.</summary>
public sealed record VisualMatchDto(
    string PetId,
    string LostEventId,
    string PetName,
    string Species,
    string? PhotoUrl,
    /// <summary>Combined similarity + geo score in [0, 1].</summary>
    float SimilarityScore,
    /// <summary>Distance in km from the probe photo location to the pet's last-seen location. Null when location is absent.</summary>
    float? DistanceKm,
    string PublicProfileUrl);

// ── Query ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Accepts a probe photo (via upload stream) and optional GPS coordinates,
/// then returns the up-to-35 active lost-pet profiles whose photo embedding
/// best resembles the probe photo, ordered by cosine similarity × geo proximity.
/// </summary>
public sealed record MatchSightingPhotoQuery(
    Stream   PhotoStream,
    string   PhotoContentType,
    double?  Lat,
    double?  Lng)
    : IRequest<Result<IReadOnlyList<VisualMatchDto>>>;

// ── Settings ──────────────────────────────────────────────────────────────────

/// <summary>Settings injected as a singleton for the visual-match feature.</summary>
/// <param name="BaseUrl">Base URL used to build public pet profile links.</param>
public sealed record VisualMatchSettings(string BaseUrl = "https://pawtrack.cr");

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class MatchSightingPhotoQueryHandler(
    IImageEmbeddingService   embeddingService,
    IVisualMatchRepository   visualMatchRepository,
    IUnitOfWork              unitOfWork,
    VisualMatchSettings      settings,
    ILogger<MatchSightingPhotoQueryHandler> logger)
    : IRequestHandler<MatchSightingPhotoQuery, Result<IReadOnlyList<VisualMatchDto>>>
{
    // Per spec: return up to 35 candidates.
    private const int   TopK                   = 35;
    // Lowered from 0.55 → 0.40 so the full 35-slot window is reachable.
    // Azure Vision 4.0 cosine scores: identical photos ≈ 0.98, different species < 0.35.
    private const float MinSimilarityThreshold = 0.40f;
    private const float CosineWeight           = 0.70f;
    private const float GeoWeight              = 0.30f;

    public async Task<Result<IReadOnlyList<VisualMatchDto>>> Handle(
        MatchSightingPhotoQuery request,
        CancellationToken       cancellationToken)
    {
        // ── 1. Vectorise the probe photo ──────────────────────────────────────
        var probeVector = await embeddingService.VectorizeStreamAsync(
            request.PhotoStream, request.PhotoContentType, cancellationToken);

        if (probeVector is null)
            return Result.Failure<IReadOnlyList<VisualMatchDto>>(
                "No se pudo analizar la imagen. Asegúrate de usar una foto clara con buena iluminación.");

        // ── 2. Load active lost-pet profiles ──────────────────────────────────
        var profiles = await visualMatchRepository.GetActiveLostPetProfilesAsync(cancellationToken);
        if (profiles.Count == 0)
            return Result.Success<IReadOnlyList<VisualMatchDto>>([]);

        // ── 3. Batch-load cached embeddings (eliminates N+1) ──────────────────
        var petIds   = profiles.Select(p => p.PetId);
        var embedded = await visualMatchRepository.GetEmbeddingsByPetIdsAsync(petIds, cancellationToken);

        // ── 4. Score each profile ─────────────────────────────────────────────
        var baseUrl          = settings.BaseUrl;
        var hasNewEmbeddings = false;

        var scored = new List<(ActiveLostPetProfile Profile, float Score, float? DistanceKm)>(profiles.Count);

        foreach (var profile in profiles)
        {
            if (profile.PhotoUrl is null) continue;

            var photoUrlHash = ComputeUrlHash(profile.PhotoUrl);
            float[] petVector;

            if (embedded.TryGetValue(profile.PetId, out var cached)
                && cached.PhotoUrlHash == photoUrlHash)
            {
                // Cache hit — deserialise stored embedding.
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
                // Cache miss or stale — call Azure Vision.
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
            if (request.Lat.HasValue && request.Lng.HasValue
                && profile.LastSeenLat.HasValue && profile.LastSeenLng.HasValue)
            {
                distKm = (float)VectorMath.HaversineKm(
                    request.Lat.Value, request.Lng.Value,
                    profile.LastSeenLat.Value, profile.LastSeenLng.Value);
            }

            var geoScore = VectorMath.GeoProximityScore(
                request.Lat, request.Lng, profile.LastSeenLat, profile.LastSeenLng);

            var combined = cosine * CosineWeight + geoScore * GeoWeight;
            scored.Add((profile, combined, distKm));
        }

        if (hasNewEmbeddings) await unitOfWork.SaveChangesAsync(cancellationToken);

        // ── 5. Build results — up to TopK (35) ────────────────────────────────
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

    // ── Helpers ───────────────────────────────────────────────────────────────

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

    internal static string ComputeUrlHash(string url)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(url));
        return Convert.ToHexString(bytes);
    }
}

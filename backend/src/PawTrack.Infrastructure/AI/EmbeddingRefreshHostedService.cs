using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Pets;
using System.Text.Json;

namespace PawTrack.Infrastructure.AI;

/// <summary>
/// Background service that pre-warms the <c>PetPhotoEmbeddings</c> cache for
/// all active lost pets.
/// <para>
/// Runs periodically (every <see cref="RefreshInterval"/>) and calls
/// <see cref="IVisualMatchRepository.GetActivePetsNeedingEmbeddingRefreshAsync"/> to find
/// pets whose embedding is missing or stale (photo changed since last vectorisation).
/// Then calls Azure Computer Vision 4.0 to generate embeddings and persists them.
/// </para>
/// <para>
/// This eliminates the cold-start latency in <c>MatchSightingPhotoQueryHandler</c>:
/// at query time all (or most) embeddings are already in <c>PetPhotoEmbeddings</c>,
/// so the query only computes the probe vector and performs in-memory cosine scoring.
/// </para>
/// </summary>
public sealed class EmbeddingRefreshHostedService(
    IServiceScopeFactory           scopeFactory,
    ILogger<EmbeddingRefreshHostedService> logger)
    : BackgroundService
{
    /// <summary>Run every 15 minutes. Adjust via <c>VisualMatch:RefreshIntervalMinutes</c> if needed.</summary>
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(15);

    /// <summary>Delay before the first run so that the app finishes starting up.</summary>
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "EmbeddingRefreshHostedService started. First run in {Delay}.",
            InitialDelay);

        await Task.Delay(InitialDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunRefreshCycleAsync(stoppingToken);
            await Task.Delay(RefreshInterval, stoppingToken);
        }
    }

    private async Task RunRefreshCycleAsync(CancellationToken ct)
    {
        // Use a scoped DI scope so EF Core DbContext (Scoped lifetime) can be resolved.
        await using var scope = scopeFactory.CreateAsyncScope();

        var visualMatchRepo = scope.ServiceProvider.GetRequiredService<IVisualMatchRepository>();
        var embeddingService = scope.ServiceProvider.GetRequiredService<IImageEmbeddingService>();
        var unitOfWork       = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        IReadOnlyList<ActiveLostPetProfile> stale;
        try
        {
            stale = await visualMatchRepo.GetActivePetsNeedingEmbeddingRefreshAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to query pets needing embedding refresh.");
            return;
        }

        if (stale.Count == 0)
        {
            logger.LogDebug("EmbeddingRefreshHostedService: all embeddings are fresh.");
            return;
        }

        logger.LogInformation(
            "EmbeddingRefreshHostedService: refreshing {Count} stale embedding(s).", stale.Count);

        var refreshed = 0;

        foreach (var profile in stale)
        {
            if (ct.IsCancellationRequested) break;
            if (profile.PhotoUrl is null) continue;

            try
            {
                var vector = await embeddingService.VectorizeUrlAsync(profile.PhotoUrl, ct);
                if (vector is null)
                {
                    logger.LogDebug(
                        "Azure Vision unavailable for pet {PetId}; will retry next cycle.",
                        profile.PetId);
                    continue;
                }

                var hash      = ComputeUrlHash(profile.PhotoUrl);
                var json      = JsonSerializer.Serialize(vector);
                var embedding = PetPhotoEmbedding.Create(profile.PetId, json, hash);

                await visualMatchRepo.UpsertEmbeddingAsync(embedding, ct);
                refreshed++;

                // Commit each embedding individually to avoid large transactions.
                await unitOfWork.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex,
                    "Failed to refresh embedding for pet {PetId}.", profile.PetId);
            }
        }

        logger.LogInformation("EmbeddingRefreshHostedService: refreshed {Count}/{Total}.",
            refreshed, stale.Count);
    }

    private static string ComputeUrlHash(string url)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(url));
        return Convert.ToHexString(bytes);
    }
}

using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.LostPets;
using PawTrack.Domain.Pets;
using PawTrack.Infrastructure.Persistence;
using System.Security.Cryptography;
using System.Text;

namespace PawTrack.Infrastructure.Sightings;

/// <summary>
/// Cross-module read repository that joins Pets, LostPetEvents and PetPhotoEmbeddings
/// to power the visual-match feature.
/// </summary>
public sealed class VisualMatchRepository(PawTrackDbContext dbContext) : IVisualMatchRepository
{
    public async Task<IReadOnlyList<ActiveLostPetProfile>> GetActiveLostPetProfilesAsync(
        CancellationToken cancellationToken = default)
    {
        // Single query: active lost events + their pet info.
        // Only returns pets that have a photo (required for vectorisation).
        var profiles = await dbContext.LostPetEvents
            .AsNoTracking()
            .Where(e => e.Status == LostPetStatus.Active)
            .Join(
                dbContext.Pets.AsNoTracking().Where(p => p.PhotoUrl != null),
                lostEvent => lostEvent.PetId,
                pet       => pet.Id,
                (lostEvent, pet) => new ActiveLostPetProfile(
                    pet.Id,
                    lostEvent.Id,
                    pet.Name,
                    pet.Species.ToString(),
                    lostEvent.LastSeenLat,
                    lostEvent.LastSeenLng,
                    pet.PhotoUrl,
                    lostEvent.ReportedAt))
            .ToListAsync(cancellationToken);

        return profiles.AsReadOnly();
    }

    public Task<PetPhotoEmbedding?> GetEmbeddingByPetIdAsync(
        Guid petId,
        CancellationToken cancellationToken = default) =>
        dbContext.PetPhotoEmbeddings
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.PetId == petId, cancellationToken);

    public async Task<Dictionary<Guid, PetPhotoEmbedding>> GetEmbeddingsByPetIdsAsync(
        IEnumerable<Guid> petIds,
        CancellationToken cancellationToken = default)
    {
        var ids = petIds.ToList();
        if (ids.Count == 0) return [];

        var rows = await dbContext.PetPhotoEmbeddings
            .AsNoTracking()
            .Where(e => ids.Contains(e.PetId))
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(e => e.PetId);
    }

    public async Task<IReadOnlyList<ActiveLostPetProfile>> GetActivePetsNeedingEmbeddingRefreshAsync(
        CancellationToken cancellationToken = default)
    {
        // Load all active profiles with a photo and their current embeddings in two queries,
        // then filter in memory to avoid a complex SQL expression for the hash comparison.
        var profiles = await GetActiveLostPetProfilesAsync(cancellationToken);
        if (profiles.Count == 0) return [];

        var petIds    = profiles.Select(p => p.PetId).ToList();
        var embedding = await GetEmbeddingsByPetIdsAsync(petIds, cancellationToken);

        var stale = profiles.Where(p =>
        {
            if (p.PhotoUrl is null) return false;
            if (!embedding.TryGetValue(p.PetId, out var cached)) return true; // missing
            var expectedHash = HashUrl(p.PhotoUrl);
            return cached.PhotoUrlHash != expectedHash;            // stale
        }).ToList();

        return stale.AsReadOnly();
    }

    public async Task UpsertEmbeddingAsync(
        PetPhotoEmbedding embedding,
        CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.PetPhotoEmbeddings
            .AnyAsync(e => e.PetId == embedding.PetId, cancellationToken);

        if (exists)
            dbContext.PetPhotoEmbeddings.Update(embedding);
        else
            await dbContext.PetPhotoEmbeddings.AddAsync(embedding, cancellationToken);
    }

    private static string HashUrl(string url)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(url));
        return Convert.ToHexString(bytes);
    }
}

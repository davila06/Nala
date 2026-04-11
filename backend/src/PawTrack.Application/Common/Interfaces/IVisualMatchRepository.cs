using PawTrack.Domain.Pets;

namespace PawTrack.Application.Common.Interfaces;

/// <summary>
/// Read-model repository that joins <c>Pets</c>, <c>LostPetEvents</c>, and
/// <c>PetPhotoEmbeddings</c> to power the visual-match feature without
/// violating module boundaries.
/// </summary>
public interface IVisualMatchRepository
{
    /// <summary>
    /// Returns every pet that currently has an active lost report and a non-null photo URL.
    /// </summary>
    Task<IReadOnlyList<ActiveLostPetProfile>> GetActiveLostPetProfilesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Returns the stored embedding for the given pet, or <c>null</c> if none exists.</summary>
    Task<PetPhotoEmbedding?> GetEmbeddingByPetIdAsync(
        Guid petId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch-loads embeddings for the given set of pet IDs in a single query.
    /// Returns a dictionary keyed by <c>PetId</c>. Missing pets have no entry.
    /// </summary>
    Task<Dictionary<Guid, PetPhotoEmbedding>> GetEmbeddingsByPetIdsAsync(
        IEnumerable<Guid> petIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns active lost-pet profiles whose stored embedding is either missing or stale
    /// (i.e. the pet's current photo URL hash does not match the cached <see cref="PetPhotoEmbedding.PhotoUrlHash"/>).
    /// Used by the background refresh service to keep the embedding cache up-to-date.
    /// </summary>
    Task<IReadOnlyList<ActiveLostPetProfile>> GetActivePetsNeedingEmbeddingRefreshAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or replaces the embedding for the given pet.
    /// Callers must follow with <see cref="IUnitOfWork.SaveChangesAsync"/> to commit.
    /// </summary>
    Task UpsertEmbeddingAsync(
        PetPhotoEmbedding embedding,
        CancellationToken cancellationToken = default);
}

// ── Read model ────────────────────────────────────────────────────────────────

/// <summary>Flattened read-only view of a pet currently reported as lost.</summary>
public sealed record ActiveLostPetProfile(
    Guid   PetId,
    Guid   LostEventId,
    string PetName,
    string Species,
    double? LastSeenLat,
    double? LastSeenLng,
    string? PhotoUrl,
    DateTimeOffset ReportedAt);

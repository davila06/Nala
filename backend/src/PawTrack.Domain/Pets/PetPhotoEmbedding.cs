using System.Text.Json;

namespace PawTrack.Domain.Pets;

/// <summary>
/// Caches the Azure Computer Vision 4.0 image embedding for a pet's profile photo.
/// Used by the visual-match feature to find lost pets whose photo resembles a sighting photo.
/// One row per pet; regenerated whenever the pet's photo URL changes.
/// </summary>
public sealed class PetPhotoEmbedding
{
    private PetPhotoEmbedding() { } // EF Core

    /// <summary>References <c>Pets.Id</c>; serves as the primary key (one row per pet).</summary>
    public Guid PetId { get; private set; }

    /// <summary>
    /// JSON-serialised <c>float[]</c> (1024 dimensions) from Azure CV 4.0 Image Retrieval.
    /// Stored as <c>nvarchar(max)</c> — ~6 KB per row.
    /// </summary>
    public string EmbeddingJson { get; private set; } = string.Empty;

    /// <summary>
    /// SHA-256 hex hash of the photo URL that was vectorised.
    /// Compared before using a cached embedding to detect stale entries when the photo changes.
    /// </summary>
    public string PhotoUrlHash { get; private set; } = string.Empty;

    /// <summary>UTC instant at which the embedding was generated.</summary>
    public DateTimeOffset GeneratedAt { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static PetPhotoEmbedding Create(
        Guid petId, string embeddingJson, string photoUrlHash) =>
        new()
        {
            PetId         = petId,
            EmbeddingJson = embeddingJson,
            PhotoUrlHash  = photoUrlHash,
            GeneratedAt   = DateTimeOffset.UtcNow,
        };

    // ── Mutation ──────────────────────────────────────────────────────────────

    /// <summary>Replaces the stored embedding (called when the pet's photo changes).</summary>
    public void Update(string embeddingJson, string photoUrlHash)
    {
        EmbeddingJson = embeddingJson;
        PhotoUrlHash  = photoUrlHash;
        GeneratedAt   = DateTimeOffset.UtcNow;
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    /// <summary>Deserialises <see cref="EmbeddingJson"/> to a float array.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the JSON is corrupt.</exception>
    public float[] DeserializeVector() =>
        JsonSerializer.Deserialize<float[]>(EmbeddingJson)
        ?? throw new InvalidOperationException(
            $"EmbeddingJson for pet {PetId} is null or corrupt.");
}

namespace PawTrack.Application.Common.Interfaces;

/// <summary>
/// Generates dense vector embeddings from images for semantic visual matching.
/// Backed by Azure Computer Vision 4.0 Image Retrieval API.
/// Returns <c>null</c> gracefully when the service is unconfigured or unavailable.
/// </summary>
public interface IImageEmbeddingService
{
    /// <summary>
    /// Vectorises an image provided as a stream (binary upload path).
    /// Returns <c>null</c> when vectorisation fails or the service is unavailable.
    /// </summary>
    Task<float[]?> VectorizeStreamAsync(
        Stream imageStream,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Vectorises an image at a publicly accessible URL (cached-embedding refresh path).
    /// Returns <c>null</c> when vectorisation fails or the service is unavailable.
    /// </summary>
    Task<float[]?> VectorizeUrlAsync(
        string imageUrl,
        CancellationToken cancellationToken = default);
}

namespace PawTrack.Application.Common.Interfaces;

/// <summary>Blob Storage abstraction — isolates Azure SDK from Application layer.</summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a file to the given container.
    /// </summary>
    /// <param name="containerName">Target container (e.g. "pet-photos").</param>
    /// <param name="blobName">Unique blob path, e.g. "{petId}/{timestamp}-{filename}".</param>
    /// <param name="stream">File content stream.</param>
    /// <param name="contentType">MIME type, e.g. "image/jpeg".</param>
    /// <returns>Public URL of the uploaded blob.</returns>
    Task<string> UploadAsync(
        string containerName,
        string blobName,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes a blob at the given URL. No-ops if the blob does not exist.</summary>
    Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads blob bytes from a previously stored blob URL.
    /// Returns <c>null</c> when the URL is invalid or the blob does not exist.
    /// </summary>
    Task<byte[]?> DownloadAsync(string blobUrl, CancellationToken cancellationToken = default);
}

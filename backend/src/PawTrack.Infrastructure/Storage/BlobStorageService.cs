using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Infrastructure.Storage;

public sealed class BlobStorageService(IConfiguration configuration) : IBlobStorageService
{
    private readonly string _connectionString =
        configuration["Azure:Storage:ConnectionString"]
        ?? throw new InvalidOperationException("Azure:Storage:ConnectionString not configured.");

    /// <summary>
    /// Containers that intentionally allow anonymous public read access.
    /// Pet and sighting photos must be publicly accessible so anyone who scans a QR code
    /// (or sees a shared link) can view the pet profile without authentication.
    /// Any container NOT in this set is created with <see cref="PublicAccessType.None"/>
    /// (private), which is the secure-by-default posture for future containers.
    /// </summary>
    private static readonly HashSet<string> _knownPublicContainers =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "pet-photos",
            "sighting-photos",
            "found-pet-photos",
            "lost-pet-photos",
        };

    public async Task<string> UploadAsync(
        string containerName,
        string blobName,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var containerClient = new BlobContainerClient(_connectionString, containerName);

        // Use public access only for known-public containers; all others default to private.
        var accessType = _knownPublicContainers.Contains(containerName)
            ? PublicAccessType.Blob
            : PublicAccessType.None;
        await containerClient.CreateIfNotExistsAsync(accessType, cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(
            stream,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);

        return blobClient.Uri.ToString();
    }

    public async Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(blobUrl)) return;

        var uri = new Uri(blobUrl);

        // Extract container and blob path from URL:
        // https://<account>.blob.core.windows.net/<container>/<blobPath>
        var segments = uri.AbsolutePath.TrimStart('/').Split('/', 2);
        if (segments.Length != 2) return;

        var containerName = segments[0];
        var blobName = segments[1];

        var containerClient = new BlobContainerClient(_connectionString, containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task<byte[]?> DownloadAsync(string blobUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(blobUrl))
            return null;

        if (!TryParseBlobUrl(blobUrl, out var containerName, out var blobName))
            return null;

        var containerClient = new BlobContainerClient(_connectionString, containerName!);
        var blobClient = containerClient.GetBlobClient(blobName!);

        if (!await blobClient.ExistsAsync(cancellationToken))
            return null;

        var download = await blobClient.DownloadContentAsync(cancellationToken);
        return download.Value.Content.ToArray();
    }

    private static bool TryParseBlobUrl(string blobUrl, out string? containerName, out string? blobName)
    {
        containerName = null;
        blobName = null;

        if (!Uri.TryCreate(blobUrl, UriKind.Absolute, out var uri))
            return false;

        var segments = uri.AbsolutePath.TrimStart('/').Split('/', 2);
        if (segments.Length != 2)
            return false;

        containerName = segments[0];
        blobName = segments[1];
        return true;
    }
}

using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Infrastructure.Storage;

public sealed class BlobStorageService(IConfiguration configuration) : IBlobStorageService
{
    private readonly BlobServiceClient _serviceClient = CreateServiceClient(configuration);
    private readonly string _publicBaseUrl = configuration["App:ApiBaseUrl"]
        ?? configuration["App:BaseUrl"]
        ?? throw new InvalidOperationException("App:ApiBaseUrl not configured.");

    public async Task<string> UploadAsync(
        string containerName,
        string blobName,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var containerClient = _serviceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(
            stream,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);

        return PublicMediaPolicy.IsPublicContainer(containerName)
            ? PublicMediaPolicy.BuildPublicUrl(_publicBaseUrl, containerName, blobName)
            : blobClient.Uri.ToString();
    }

    public async Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(blobUrl)) return;

        if (!TryParseBlobUrl(blobUrl, out var containerName, out var blobName)) return;

        var containerClient = _serviceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task<byte[]?> DownloadAsync(string blobUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(blobUrl))
            return null;

        if (!TryParseBlobUrl(blobUrl, out var containerName, out var blobName))
            return null;

        var containerClient = _serviceClient.GetBlobContainerClient(containerName!);
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

        var path = uri.AbsolutePath.TrimStart('/');
        const string publicMediaPrefix = "api/public/media/";
        if (path.StartsWith(publicMediaPrefix, StringComparison.OrdinalIgnoreCase))
            path = path[publicMediaPrefix.Length..];

        var segments = path.Split('/', 2);
        if (segments.Length != 2)
            return false;

        containerName = Uri.UnescapeDataString(segments[0]);
        blobName = Uri.UnescapeDataString(segments[1]);
        return true;
    }

    public static BlobServiceClient CreateServiceClient(IConfiguration configuration)
    {
        var serviceUri = configuration["Azure:Storage:ServiceUri"];
        if (!string.IsNullOrWhiteSpace(serviceUri))
            return new BlobServiceClient(new Uri(serviceUri), new DefaultAzureCredential());

        var connectionString = configuration["Azure:Storage:ConnectionString"]
            ?? throw new InvalidOperationException("Azure Storage ServiceUri or ConnectionString must be configured.");
        return new BlobServiceClient(connectionString);
    }
}

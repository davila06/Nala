using PawTrack.Application.Common.Interfaces;

namespace PawTrack.IntegrationTests.Infrastructure;

public sealed class StubBlobStorageService : IBlobStorageService
{
    public Task<string> UploadAsync(string containerName, string blobName, System.IO.Stream stream,
        string contentType, CancellationToken cancellationToken = default) =>
        Task.FromResult($"https://test-storage/{containerName}/{blobName}");

    public Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<byte[]?> DownloadAsync(string blobUrl, CancellationToken cancellationToken = default) =>
        Task.FromResult<byte[]?>(null);
}

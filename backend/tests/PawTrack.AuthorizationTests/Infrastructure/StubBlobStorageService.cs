using PawTrack.Application.Common.Interfaces;

namespace PawTrack.AuthorizationTests.Infrastructure;

public sealed class StubBlobStorageService : IBlobStorageService
{
    public Task<string> UploadAsync(
        string containerName,
        string blobName,
        Stream stream,
        string contentType,
        CancellationToken cancellationToken = default) =>
        Task.FromResult($"https://authorization-tests/{containerName}/{blobName}");

    public Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<byte[]?> DownloadAsync(string blobUrl, CancellationToken cancellationToken = default) =>
        Task.FromResult<byte[]?>(null);
}

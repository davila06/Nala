using PawTrack.Application.Certificates.Interfaces;

namespace PawTrack.IntegrationTests.Infrastructure;

[Collection("Integration")]
public sealed class StubCertificateService : ICertificateService
{
    public Task<string> GenerateAndStoreAsync(CertificatePdfData data, CancellationToken cancellationToken = default) =>
        Task.FromResult("https://test-storage/certs/test.pdf");
}

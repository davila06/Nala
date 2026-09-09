using PawTrack.Application.Certificates.Interfaces;

namespace PawTrack.IntegrationTests.Infrastructure;

[Collection("Integration")]
public sealed class StubCertificateService : ICertificateService
{
    public Task<CertificateArtifact> GenerateAndStoreAsync(CertificatePdfData data, CancellationToken cancellationToken = default) =>
        Task.FromResult(new CertificateArtifact("https://test-storage/certs/test.pdf", null, null));
}

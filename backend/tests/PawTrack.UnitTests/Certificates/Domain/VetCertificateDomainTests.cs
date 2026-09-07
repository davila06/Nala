using FluentAssertions;
using PawTrack.Domain.Certificates;

namespace PawTrack.UnitTests.Certificates.Domain;

public sealed class VetCertificateDomainTests
{
    private static VetCertificate MakeCert() =>
        VetCertificate.Issue(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CertificateType.Vaccination, "ABCD1234");

    [Fact]
    public void Issue_ValidInputs_IsValidAndNotRevoked()
    {
        var cert = MakeCert();

        cert.IsRevoked.Should().BeFalse();
        cert.IsValid.Should().BeTrue();
        cert.VerificationCode.Should().Be("ABCD1234");
        cert.PdfUrl.Should().BeNull();
    }

    [Fact]
    public void Issue_WithFutureValidUntil_IsValid()
    {
        var cert = VetCertificate.Issue(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            CertificateType.Vaccination, "ABCD1234",
            validUntil: DateTimeOffset.UtcNow.AddYears(1));

        cert.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Issue_WithPastValidUntil_IsNotValid()
    {
        var cert = VetCertificate.Issue(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            CertificateType.Vaccination, "ABCD1234",
            validUntil: DateTimeOffset.UtcNow.AddDays(-1));

        cert.IsValid.Should().BeFalse();
    }

    [Fact]
    public void SetPdfUrl_SetsBlobUrl()
    {
        var cert = MakeCert();
        cert.SetPdfUrl("https://storage/cert.pdf");
        cert.PdfUrl.Should().Be("https://storage/cert.pdf");
    }

    [Fact]
    public void Revoke_SetsIsRevokedAndInvalidates()
    {
        var cert = MakeCert();
        cert.Revoke();
        cert.IsRevoked.Should().BeTrue();
        cert.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Revoke_WithReason_CapturesEnterpriseRevocationMetadata()
    {
        var cert = MakeCert();
        var revokedBy = Guid.NewGuid();

        var result = cert.Revoke(revokedBy, "Error en lote de vacuna");

        result.IsSuccess.Should().BeTrue();
        cert.IsRevoked.Should().BeTrue();
        cert.IsValid.Should().BeFalse();
        cert.RevokedByUserId.Should().Be(revokedBy);
        cert.RevocationReason.Should().Be("Error en lote de vacuna");
        cert.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public void Revoke_WithoutReason_ReturnsFailure()
    {
        var cert = MakeCert();

        var result = cert.Revoke(Guid.NewGuid(), " ");

        result.IsFailure.Should().BeTrue();
        cert.IsRevoked.Should().BeFalse();
    }
}

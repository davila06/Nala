using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Certificates.Queries.VerifyCertificate;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Certificates;

namespace PawTrack.UnitTests.Certificates.Queries;

public sealed class VerifyCertificateQueryHandlerTests
{
    private readonly ICertificateRepository _certificates = Substitute.For<ICertificateRepository>();
    private readonly IVaccinePassportRepository _passports = Substitute.For<IVaccinePassportRepository>();
    private readonly ICertificateAuditLogRepository _auditLogs = Substitute.For<ICertificateAuditLogRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IBlobStorageService _blobStorage = Substitute.For<IBlobStorageService>();
    private readonly ICertificateDigitalSigner _digitalSigner = Substitute.For<ICertificateDigitalSigner>();

    [Fact]
    public async Task Handle_ExistingPassportCertificate_ReturnsPublicVerificationDtoFromSnapshots()
    {
        var certificate = VetCertificate.Issue(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            CertificateType.VaccinePassport,
            "ABCD1234",
            notes: "Nota interna sensible");
        certificate.SetPdfUrl("https://storage.example/certificates/private.pdf");
        certificate.SetSignature("https://storage.example/certificates/private.sig", "RSA-SHA256-PKCS1-KeyVault");
        var passport = VaccinePassport.Issue(
            certificate.Id,
            certificate.PetId,
            certificate.ClinicId,
            Guid.NewGuid(),
            new VaccinePassportPetSnapshot("Nala", "Dog", null, null, "Dorado", "123456789012345", "Denis"),
            new VaccinePassportIssuerSnapshot("VetSalud", "SENASA-12345", "Dra. Rivera", "VET-12345"),
            [new VaccinePassportVaccine("Rabia", "Brand", "LOT-1", new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1))],
            null,
            new DateOnly(2027, 1, 1),
            certificate.VerificationCode);

        _certificates.GetByVerificationCodeAsync("ABCD1234", Arg.Any<CancellationToken>()).Returns(certificate);
        _passports.GetByCertificateIdAsync(certificate.Id, Arg.Any<CancellationToken>()).Returns(passport);
        _blobStorage.DownloadAsync(certificate.PdfUrl!, Arg.Any<CancellationToken>()).Returns([1, 2, 3]);
        _blobStorage.DownloadAsync(certificate.SignatureUrl!, Arg.Any<CancellationToken>())
            .Returns(System.Text.Encoding.UTF8.GetBytes(Convert.ToBase64String([4, 5, 6])));
        _digitalSigner.VerifyAsync(Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await new VerifyCertificateQueryHandler(_certificates, _passports, _auditLogs, _unitOfWork, _blobStorage, _digitalSigner)
            .Handle(new VerifyCertificateQuery("ABCD1234"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.VerificationCode.Should().Be("ABCD1234");
        result.Value.Type.Should().Be(CertificateType.VaccinePassport.ToString());
        result.Value.PetName.Should().Be("Nala");
        result.Value.PetSpecies.Should().Be("Dog");
        result.Value.ClinicName.Should().Be("VetSalud");
        result.Value.SignatureVerified.Should().BeTrue();
        await _auditLogs.Received(1).AddAsync(
            Arg.Is<CertificateAuditLog>(log => log.Action == CertificateAuditAction.VerifiedPublicly && log.CertificateId == certificate.Id),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

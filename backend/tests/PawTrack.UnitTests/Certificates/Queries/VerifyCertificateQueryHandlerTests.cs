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

        var result = await new VerifyCertificateQueryHandler(_certificates, _passports, _auditLogs, _unitOfWork)
            .Handle(new VerifyCertificateQuery("ABCD1234"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.VerificationCode.Should().Be("ABCD1234");
        result.Value.Type.Should().Be(CertificateType.VaccinePassport.ToString());
        result.Value.PetName.Should().Be("Nala");
        result.Value.PetSpecies.Should().Be("Dog");
        result.Value.ClinicName.Should().Be("VetSalud");
        await _auditLogs.Received(1).AddAsync(
            Arg.Is<CertificateAuditLog>(log => log.Action == CertificateAuditAction.VerifiedPublicly && log.CertificateId == certificate.Id),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

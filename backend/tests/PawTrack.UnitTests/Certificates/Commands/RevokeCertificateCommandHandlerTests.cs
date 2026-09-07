using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Certificates.Commands.RevokeCertificate;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Clinics;

namespace PawTrack.UnitTests.Certificates.Commands;

public sealed class RevokeCertificateCommandHandlerTests
{
    private readonly ICertificateRepository _certificates = Substitute.For<ICertificateRepository>();
    private readonly IClinicRepository _clinics = Substitute.For<IClinicRepository>();
    private readonly ICertificateAuditLogRepository _auditLogs = Substitute.For<ICertificateAuditLogRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private RevokeCertificateCommandHandler BuildHandler() => new(_certificates, _clinics, _auditLogs, _unitOfWork);

    [Fact]
    public async Task Handle_IssuingClinicOwnerRevokesWithReason_Succeeds()
    {
        var clinicOwnerId = Guid.NewGuid();
        var clinic = Clinic.Create(clinicOwnerId, "VetSalud", "SENASA-12345", "Heredia", 10m, -84.1m, "vet@example.com");
        var certificate = VetCertificate.Issue(Guid.NewGuid(), clinic.Id, clinicOwnerId, CertificateType.VaccinePassport, "ABCD1234");

        _certificates.GetByIdAsync(certificate.Id, Arg.Any<CancellationToken>()).Returns(certificate);
        _clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);

        var result = await BuildHandler().Handle(
            new RevokeCertificateCommand(certificate.Id, clinicOwnerId, IsAdmin: false, "Error en datos sanitarios"),
            default);

        result.IsSuccess.Should().BeTrue();
        certificate.IsRevoked.Should().BeTrue();
        certificate.RevocationReason.Should().Be("Error en datos sanitarios");
        _certificates.Received(1).Update(certificate);
        await _auditLogs.Received(1).AddAsync(
            Arg.Is<CertificateAuditLog>(log => log.Action == CertificateAuditAction.Revoked && log.ActorUserId == clinicOwnerId),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonIssuingClinicOwner_ReturnsAccessDenied()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "VetSalud", "SENASA-12345", "Heredia", 10m, -84.1m, "vet@example.com");
        var certificate = VetCertificate.Issue(Guid.NewGuid(), clinic.Id, clinic.UserId, CertificateType.VaccinePassport, "ABCD1234");

        _certificates.GetByIdAsync(certificate.Id, Arg.Any<CancellationToken>()).Returns(certificate);
        _clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);

        var result = await BuildHandler().Handle(
            new RevokeCertificateCommand(certificate.Id, Guid.NewGuid(), IsAdmin: false, "Error en datos sanitarios"),
            default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Acceso denegado.");
        certificate.IsRevoked.Should().BeFalse();
        _certificates.DidNotReceive().Update(Arg.Any<VetCertificate>());
    }
}

using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Certificates.Queries.DownloadCertificatePdf;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Certificates.Queries;

public sealed class DownloadCertificatePdfQueryHandlerTests
{
    private readonly ICertificateRepository _certificates = Substitute.For<ICertificateRepository>();
    private readonly IPetRepository _pets = Substitute.For<IPetRepository>();
    private readonly IFamilyRepository _family = Substitute.For<IFamilyRepository>();
    private readonly IClinicRepository _clinics = Substitute.For<IClinicRepository>();
    private readonly IBlobStorageService _blobStorage = Substitute.For<IBlobStorageService>();
    private readonly ICertificateAuditLogRepository _auditLogs = Substitute.For<ICertificateAuditLogRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private DownloadCertificatePdfQueryHandler BuildHandler() => new(
        _certificates,
        _pets,
        _family,
        _clinics,
        _blobStorage,
        _auditLogs,
        _unitOfWork);

    [Fact]
    public async Task Handle_PetOwnerDownloads_ReturnsPdfBytesAndAuditsDownload()
    {
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Nala", PetSpecies.Dog, null, null);
        var certificate = VetCertificate.Issue(pet.Id, Guid.NewGuid(), Guid.NewGuid(), CertificateType.VaccinePassport, "ABCD1234");
        certificate.SetPdfUrl("https://storage.example/certificates/cert.pdf");

        _certificates.GetByIdAsync(certificate.Id, Arg.Any<CancellationToken>()).Returns(certificate);
        _pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _blobStorage.DownloadAsync(certificate.PdfUrl!, Arg.Any<CancellationToken>()).Returns([1, 2, 3]);

        var result = await BuildHandler().Handle(
            new DownloadCertificatePdfQuery(certificate.Id, ownerId, IsAdmin: false),
            default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Bytes.Should().Equal(1, 2, 3);
        result.Value.FileName.Should().Be($"pawtrack-certificate-{certificate.VerificationCode}.pdf");
        await _auditLogs.Received(1).AddAsync(
            Arg.Is<CertificateAuditLog>(log => log.Action == CertificateAuditAction.Downloaded && log.ActorUserId == ownerId),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnrelatedUser_ReturnsAccessDenied()
    {
        var pet = Pet.Create(Guid.NewGuid(), "Nala", PetSpecies.Dog, null, null);
        var certificate = VetCertificate.Issue(pet.Id, Guid.NewGuid(), Guid.NewGuid(), CertificateType.VaccinePassport, "ABCD1234");
        certificate.SetPdfUrl("https://storage.example/certificates/cert.pdf");

        _certificates.GetByIdAsync(certificate.Id, Arg.Any<CancellationToken>()).Returns(certificate);
        _pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _family.GetActiveMemberIdsAsync(pet.OwnerId, Arg.Any<CancellationToken>()).Returns([]);

        var result = await BuildHandler().Handle(
            new DownloadCertificatePdfQuery(certificate.Id, Guid.NewGuid(), IsAdmin: false),
            default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Acceso denegado.");
        await _blobStorage.DidNotReceive().DownloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}

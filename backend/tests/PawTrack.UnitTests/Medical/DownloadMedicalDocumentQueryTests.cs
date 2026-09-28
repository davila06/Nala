using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Medical;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Medical;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Medical;

public sealed class DownloadMedicalDocumentQueryTests
{
    [Fact]
    public async Task OwnerDownloadsDocumentThroughBlobServiceAndAudit()
    {
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Milo", PetSpecies.Dog, null, null);
        var record = MedicalRecord.Create(pet.Id, ownerId, MedicalRecordType.Other,
            new DateOnly(2026, 9, 28), "Exam", null, null, null);
        record.SetDocumentUrl("https://storage.invalid/medical-docs/pet/exam.pdf",
            MedicalDocumentKind.LaboratoryResult, "application/pdf");
        var medical = Substitute.For<IMedicalRepository>();
        var pets = Substitute.For<IPetRepository>();
        var subscriptions = Substitute.For<ISubscriptionService>();
        var blob = Substitute.For<IBlobStorageService>();
        var audit = Substitute.For<IAuditLogRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        medical.GetByIdAsync(record.Id, Arg.Any<CancellationToken>()).Returns(record);
        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        subscriptions.IsFamiliaAsync(ownerId, Arg.Any<CancellationToken>()).Returns(true);
        blob.DownloadAsync(record.DocumentUrl!, Arg.Any<CancellationToken>()).Returns([0x25, 0x50, 0x44, 0x46]);
        var handler = new DownloadMedicalDocumentQueryHandler(
            medical, pets, Substitute.For<IFamilyRepository>(), subscriptions,
            Substitute.For<IClinicRepository>(), Substitute.For<IClinicScanRepository>(),
            Substitute.For<IClinicMedicalAccessGrantRepository>(), Substitute.For<IClinicMedicalAccessLogRepository>(),
            blob, audit, uow);

        var result = await handler.Handle(new DownloadMedicalDocumentQuery(pet.Id, record.Id, ownerId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ContentType.Should().Be("application/pdf");
        result.Value.FileName.Should().EndWith(".pdf");
        result.Value.Bytes.Should().Equal(0x25, 0x50, 0x44, 0x46);
        await audit.Received(1).AddAsync(Arg.Any<PawTrack.Domain.Audit.AuditLogEntry>(), Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnrelatedUserCannotDownloadDocumentOrReachBlobStorage()
    {
        var ownerId = Guid.NewGuid();
        var outsiderId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Milo", PetSpecies.Dog, null, null);
        var record = MedicalRecord.Create(pet.Id, ownerId, MedicalRecordType.Other,
            new DateOnly(2026, 9, 28), "Exam", null, null, null);
        record.SetDocumentUrl("https://storage.invalid/medical-docs/pet/exam.pdf", contentType: "application/pdf");
        var medical = Substitute.For<IMedicalRepository>();
        var pets = Substitute.For<IPetRepository>();
        var blob = Substitute.For<IBlobStorageService>();
        medical.GetByIdAsync(record.Id, Arg.Any<CancellationToken>()).Returns(record);
        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        var handler = new DownloadMedicalDocumentQueryHandler(
            medical, pets, Substitute.For<IFamilyRepository>(), Substitute.For<ISubscriptionService>(),
            Substitute.For<IClinicRepository>(), Substitute.For<IClinicScanRepository>(),
            Substitute.For<IClinicMedicalAccessGrantRepository>(), Substitute.For<IClinicMedicalAccessLogRepository>(),
            blob, Substitute.For<IAuditLogRepository>(), Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(new DownloadMedicalDocumentQuery(pet.Id, record.Id, outsiderId), default);

        result.IsFailure.Should().BeTrue();
        await blob.DidNotReceive().DownloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}

using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Clinics.Commands.AddClinicMedicalRecord;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Medical;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Medical;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Clinics;

public sealed class AddClinicMedicalRecordCommandHandlerTests
{
    [Fact]
    public async Task Handle_QrInputTakesPrecedenceOverForgedPetId()
    {
        var clinicUserId = Guid.NewGuid();
        var clinic = Clinic.Create(clinicUserId, "Vet Salud", "SENASA-123", "San Jose", 9.93m, -84.08m, "vet@example.com");
        clinic.Activate();
        var qrPet = Pet.Create(Guid.NewGuid(), "QR Pet", PetSpecies.Dog, null, null);
        var forgedPet = Pet.Create(Guid.NewGuid(), "Forged", PetSpecies.Cat, null, null);
        var clinics = Substitute.For<IClinicRepository>();
        var scans = Substitute.For<IClinicScanRepository>();
        var grants = Substitute.For<IClinicMedicalAccessGrantRepository>();
        var pets = Substitute.For<IPetRepository>();
        var records = Substitute.For<IMedicalRepository>();
        var qr = $"https://pawtrack.cr/p/{qrPet.Id}";

        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        pets.GetByIdAsync(qrPet.Id, Arg.Any<CancellationToken>()).Returns(qrPet);
        pets.GetByIdAsync(forgedPet.Id, Arg.Any<CancellationToken>()).Returns(forgedPet);
        scans.HasRecentScanAsync(clinic.Id, forgedPet.Id, 90, Arg.Any<CancellationToken>()).Returns(true);
        records.AddAsync(Arg.Any<MedicalRecord>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var handler = new AddClinicMedicalRecordCommandHandler(
            clinics, scans, grants, pets, Substitute.For<IUserRepository>(), records,
            Substitute.For<INotificationDispatcher>(), Substitute.For<IBlobStorageService>(), Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(new AddClinicMedicalRecordCommand(
            clinic.Id, clinicUserId, forgedPet.Id, qr, ScanInputType.Qr,
            MedicalRecordType.Checkup, DateOnly.FromDateTime(DateTime.UtcNow), "Consulta", null, null, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PetId.Should().Be(qrPet.Id);
        await records.Received(1).AddAsync(
            Arg.Is<MedicalRecord>(record => record.PetId == qrPet.Id),
            Arg.Any<CancellationToken>());
        await records.DidNotReceive().AddAsync(
            Arg.Is<MedicalRecord>(record => record.PetId == forgedPet.Id),
            Arg.Any<CancellationToken>());
    }
}
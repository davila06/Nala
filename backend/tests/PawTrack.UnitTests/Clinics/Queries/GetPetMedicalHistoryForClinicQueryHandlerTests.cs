using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Clinics.Queries.GetPetMedicalHistoryForClinic;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Clinics.Queries;

public sealed class GetPetMedicalHistoryForClinicQueryHandlerTests
{
    [Fact]
    public async Task Handle_RecordsTheAuthenticatedClinicUserAsAuditActor()
    {
        var clinicUserId = Guid.NewGuid();
        var clinic = Clinic.Create(
            clinicUserId, "Vet Salud", "SENASA-123", "San Jose", 9.93m, -84.08m, "vet@example.com");
        clinic.Activate();
        var pet = Pet.Create(Guid.NewGuid(), "Max", PetSpecies.Dog, null, null);

        var clinics = Substitute.For<IClinicRepository>();
        var scans = Substitute.For<IClinicScanRepository>();
        var grants = Substitute.For<IClinicMedicalAccessGrantRepository>();
        var pets = Substitute.For<IPetRepository>();
        var medical = Substitute.For<IMedicalRepository>();
        var logs = Substitute.For<IClinicMedicalAccessLogRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        scans.HasRecentScanAsync(clinic.Id, pet.Id, 90, Arg.Any<CancellationToken>()).Returns(true);
        scans.GetLastScanDateAsync(clinic.Id, pet.Id, Arg.Any<CancellationToken>()).Returns(DateTimeOffset.UtcNow);
        medical.GetByPetIdAsync(pet.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<PawTrack.Domain.Medical.MedicalRecord>());

        PawTrack.Domain.Medical.ClinicMedicalAccessLog? capturedLog = null;
        logs.AddAsync(
                Arg.Do<PawTrack.Domain.Medical.ClinicMedicalAccessLog>(log => capturedLog = log),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var handler = new GetPetMedicalHistoryForClinicQueryHandler(
            clinics, scans, grants, pets, medical, logs, unitOfWork);

        var result = await handler.Handle(
            new GetPetMedicalHistoryForClinicQuery(clinic.Id, pet.Id, null, null, clinicUserId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedLog.Should().NotBeNull();
        capturedLog!.ClinicId.Should().Be(clinic.Id);
        capturedLog.AccessedByUserId.Should().Be(clinicUserId);
        capturedLog.Operation.Should().Be("read_medical_history");
        capturedLog.Permission.Should().Be("read");
        capturedLog.AccessMethod.Should().Be("recent_scan");
        capturedLog.Outcome.Should().Be("allowed");
        capturedLog.Reason.Should().NotBeNullOrWhiteSpace();
    }

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
        var medical = Substitute.For<IMedicalRepository>();
        var logs = Substitute.For<IClinicMedicalAccessLogRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var qr = $"https://pawtrack.cr/p/{qrPet.Id}";

        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        pets.GetByIdAsync(qrPet.Id, Arg.Any<CancellationToken>()).Returns(qrPet);
        pets.GetByIdAsync(forgedPet.Id, Arg.Any<CancellationToken>()).Returns(forgedPet);
        medical.GetByPetIdAsync(qrPet.Id, Arg.Any<CancellationToken>()).Returns(Array.Empty<PawTrack.Domain.Medical.MedicalRecord>());
        logs.AddAsync(Arg.Any<PawTrack.Domain.Medical.ClinicMedicalAccessLog>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var handler = new GetPetMedicalHistoryForClinicQueryHandler(clinics, scans, grants, pets, medical, logs, uow);
        var result = await handler.Handle(new GetPetMedicalHistoryForClinicQuery(
            clinic.Id, forgedPet.Id, qr, ScanInputType.Qr, clinicUserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PetId.Should().Be(qrPet.Id);
        await scans.Received(1).AddAsync(Arg.Is<ClinicScan>(scan => scan.MatchedPetId == qrPet.Id), Arg.Any<CancellationToken>());
        await medical.Received(1).GetByPetIdAsync(qrPet.Id, Arg.Any<CancellationToken>());
        await medical.DidNotReceive().GetByPetIdAsync(forgedPet.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ClinicUserDoesNotOwnClinic_ReturnsFailure()
    {
        var realClinicUserId = Guid.NewGuid();
        var attackerUserId = Guid.NewGuid();
        var clinic = Clinic.Create(realClinicUserId, "Vet Salud", "SENASA-123", "San Jose", 9.93m, -84.08m, "vet@example.com");
        clinic.Activate();
        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var handler = new GetPetMedicalHistoryForClinicQueryHandler(
            clinics,
            Substitute.For<IClinicScanRepository>(),
            Substitute.For<IClinicMedicalAccessGrantRepository>(),
            Substitute.For<IPetRepository>(),
            Substitute.For<IMedicalRepository>(),
            Substitute.For<IClinicMedicalAccessLogRepository>(),
            Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(
            new GetPetMedicalHistoryForClinicQuery(clinic.Id, Guid.NewGuid(), null, null, attackerUserId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WriteOnlyGrant_DoesNotPermitReadingHistory()
    {
        var clinicUserId = Guid.NewGuid();
        var clinic = Clinic.Create(clinicUserId, "Vet Salud", "SENASA-123", "San Jose", 9.93m, -84.08m, "vet@example.com");
        clinic.Activate();
        var pet = Pet.Create(Guid.NewGuid(), "Max", PetSpecies.Dog, null, null);
        var (grant, code) = PawTrack.Domain.Medical.ClinicMedicalAccessGrant.Generate(
            pet.Id, clinic.Id, pet.OwnerId, "Owner", permissions: [PawTrack.Domain.Medical.ClinicMedicalAccessPermission.Write]);
        grant.TryAccept(code);
        var clinics = Substitute.For<IClinicRepository>();
        var scans = Substitute.For<IClinicScanRepository>();
        var grants = Substitute.For<IClinicMedicalAccessGrantRepository>();
        var pets = Substitute.For<IPetRepository>();
        var medical = Substitute.For<IMedicalRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        scans.HasRecentScanAsync(clinic.Id, pet.Id, 90, Arg.Any<CancellationToken>()).Returns(false);
        grants.GetActiveGrantAsync(clinic.Id, pet.Id, Arg.Any<CancellationToken>()).Returns(grant);
        var handler = new GetPetMedicalHistoryForClinicQueryHandler(
            clinics, scans, grants, pets, medical,
            Substitute.For<IClinicMedicalAccessLogRepository>(), Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(
            new GetPetMedicalHistoryForClinicQuery(clinic.Id, pet.Id, null, null, clinicUserId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await medical.DidNotReceive().GetByPetIdAsync(pet.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DeniedAccess_RecordsDeniedAuditOutcome()
    {
        var clinicUserId = Guid.NewGuid();
        var clinic = Clinic.Create(clinicUserId, "Vet Salud", "SENASA-123", "San Jose", 9.93m, -84.08m, "vet@example.com");
        clinic.Activate();
        var pet = Pet.Create(Guid.NewGuid(), "Max", PetSpecies.Dog, null, null);
        var logs = Substitute.For<IClinicMedicalAccessLogRepository>();
        PawTrack.Domain.Medical.ClinicMedicalAccessLog? captured = null;
        logs.AddAsync(Arg.Do<PawTrack.Domain.Medical.ClinicMedicalAccessLog>(log => captured = log), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var pets = Substitute.For<IPetRepository>();
        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);

        var handler = new GetPetMedicalHistoryForClinicQueryHandler(
            clinics,
            Substitute.For<IClinicScanRepository>(),
            Substitute.For<IClinicMedicalAccessGrantRepository>(),
            pets,
            Substitute.For<IMedicalRepository>(),
            logs,
            Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(
            new GetPetMedicalHistoryForClinicQuery(clinic.Id, pet.Id, null, null, clinicUserId), default);

        result.IsFailure.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.Outcome.Should().Be("denied");
        captured.Permission.Should().Be("read");
    }
}
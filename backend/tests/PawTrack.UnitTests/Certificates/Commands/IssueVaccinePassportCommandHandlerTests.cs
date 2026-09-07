using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Certificates.Commands;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Pets;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Certificates.Commands;

public sealed class IssueVaccinePassportCommandHandlerTests
{
    private readonly ICertificateRepository _certificates = Substitute.For<ICertificateRepository>();
    private readonly ICertificateService _certificateService = Substitute.For<ICertificateService>();
    private readonly IPetRepository _pets = Substitute.For<IPetRepository>();
    private readonly IClinicRepository _clinics = Substitute.For<IClinicRepository>();
    private readonly IClinicMedicalAccessGrantRepository _grants = Substitute.For<IClinicMedicalAccessGrantRepository>();
    private readonly IClinicVerificationRepository _clinicVerifications = Substitute.For<IClinicVerificationRepository>();
    private readonly IClinicVeterinarianRepository _veterinarians = Substitute.For<IClinicVeterinarianRepository>();
    private readonly IVaccinePassportRepository _passports = Substitute.For<IVaccinePassportRepository>();
    private readonly ICertificateAuditLogRepository _auditLogs = Substitute.For<ICertificateAuditLogRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ISubscriptionRepository _subscriptions = Substitute.For<ISubscriptionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private IssueVaccinePassportCommandHandler BuildHandler() => new(
        _certificates,
        _certificateService,
        _pets,
        _clinics,
        _grants,
        _clinicVerifications,
        _veterinarians,
        _passports,
        _auditLogs,
        _users,
        _subscriptions,
        _unitOfWork);

    private static ClinicVerification MakeVerifiedClinic(Clinic clinic)
    {
        var verification = ClinicVerification.Submit(clinic.Id, clinic.LicenseNumber, clinic.UserId);
        verification.AttachDocument("https://storage.example/verification.pdf");
        verification.Verify(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)), null);
        return verification;
    }

    [Fact]
    public async Task Handle_RequestingUserDoesNotOwnClinic_ReturnsFailureWithoutIssuingCertificate()
    {
        var clinicOwnerId = Guid.NewGuid();
        var requestingUserId = Guid.NewGuid();
        var petOwnerId = Guid.NewGuid();
        var clinic = Clinic.Create(clinicOwnerId, "VetSalud", "SENASA-12345", "Heredia", 10m, -84.1m, "vet@example.com");
        clinic.Activate();
        var subscription = Subscription.CreateForClinic(clinic.Id, clinicOwnerId, SubscriptionTier.ClinicPartner, "PASS1234", 35000m);
        subscription.Activate();
        var pet = Pet.Create(petOwnerId, "Nala", PetSpecies.Dog, null, null);

        _subscriptions.GetActiveForClinicAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(subscription);
        _pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var verification = MakeVerifiedClinic(clinic);
        var veterinarian = ClinicVeterinarian.Create(clinic.Id, "Dra. Rivera", "VET-12345");
        _clinicVerifications.GetActiveForClinicAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(verification);
        _veterinarians.GetByIdAsync(veterinarian.Id, Arg.Any<CancellationToken>()).Returns(veterinarian);
        _certificateService.GenerateAndStoreAsync(Arg.Any<CertificatePdfData>(), Arg.Any<CancellationToken>())
            .Returns("https://storage.example/certificates/passport.pdf");

        var result = await BuildHandler().Handle(
            new IssueVaccinePassportCommand(
                pet.Id,
                clinic.Id,
                requestingUserId,
                veterinarian.Id,
                "Dra. Rivera",
                "VET-12345",
                "Dorado",
                [new PassportVaccineEntryInput("Rabia", "Brand", "LOT-1", new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1))],
                null),
            default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Acceso denegado.");
        await _certificates.DidNotReceive().AddAsync(Arg.Any<VetCertificate>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ClinicHasNoMedicalGrantForPet_ReturnsFailureWithoutIssuingCertificate()
    {
        var clinicOwnerId = Guid.NewGuid();
        var petOwnerId = Guid.NewGuid();
        var clinic = Clinic.Create(clinicOwnerId, "VetSalud", "SENASA-12345", "Heredia", 10m, -84.1m, "vet@example.com");
        clinic.Activate();
        var subscription = Subscription.CreateForClinic(clinic.Id, clinicOwnerId, SubscriptionTier.ClinicPartner, "PASS1234", 35000m);
        subscription.Activate();
        var pet = Pet.Create(petOwnerId, "Nala", PetSpecies.Dog, null, null);

        _subscriptions.GetActiveForClinicAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(subscription);
        _pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var verification = MakeVerifiedClinic(clinic);
        var veterinarian = ClinicVeterinarian.Create(clinic.Id, "Dra. Rivera", "VET-12345");
        _clinicVerifications.GetActiveForClinicAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(verification);
        _veterinarians.GetByIdAsync(veterinarian.Id, Arg.Any<CancellationToken>()).Returns(veterinarian);
        _certificateService.GenerateAndStoreAsync(Arg.Any<CertificatePdfData>(), Arg.Any<CancellationToken>())
            .Returns("https://storage.example/certificates/passport.pdf");

        var result = await BuildHandler().Handle(
            new IssueVaccinePassportCommand(
                pet.Id,
                clinic.Id,
                clinicOwnerId,
                veterinarian.Id,
                "Dra. Rivera",
                "VET-12345",
                "Dorado",
                [new PassportVaccineEntryInput("Rabia", "Brand", "LOT-1", new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1))],
                null),
            default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("La clínica no tiene acceso activo al expediente de esta mascota.");
        await _certificates.DidNotReceive().AddAsync(Arg.Any<VetCertificate>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ClinicIsNotActive_ReturnsFailureWithoutIssuingCertificate()
    {
        var clinicOwnerId = Guid.NewGuid();
        var petOwnerId = Guid.NewGuid();
        var clinic = Clinic.Create(clinicOwnerId, "VetSalud", "SENASA-12345", "Heredia", 10m, -84.1m, "vet@example.com");
        var subscription = Subscription.CreateForClinic(clinic.Id, clinicOwnerId, SubscriptionTier.ClinicPartner, "PASS1234", 35000m);
        subscription.Activate();
        var pet = Pet.Create(petOwnerId, "Nala", PetSpecies.Dog, null, null);

        _subscriptions.GetActiveForClinicAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(subscription);
        _pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        _grants.HasActiveGrantAsync(clinic.Id, pet.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await BuildHandler().Handle(
            new IssueVaccinePassportCommand(
                pet.Id,
                clinic.Id,
                clinicOwnerId,
                Guid.NewGuid(),
                "Dra. Rivera",
                "VET-12345",
                "Dorado",
                [new PassportVaccineEntryInput("Rabia", "Brand", "LOT-1", new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1))],
                null),
            default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("La clínica no está activa.");
        await _certificates.DidNotReceive().AddAsync(Arg.Any<VetCertificate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ClinicIsNotVerified_ReturnsFailureWithoutIssuingCertificate()
    {
        var clinicOwnerId = Guid.NewGuid();
        var petOwnerId = Guid.NewGuid();
        var clinic = Clinic.Create(clinicOwnerId, "VetSalud", "SENASA-12345", "Heredia", 10m, -84.1m, "vet@example.com");
        clinic.Activate();
        var subscription = Subscription.CreateForClinic(clinic.Id, clinicOwnerId, SubscriptionTier.ClinicPartner, "PASS1234", 35000m);
        subscription.Activate();
        var pet = Pet.Create(petOwnerId, "Nala", PetSpecies.Dog, null, null);

        _subscriptions.GetActiveForClinicAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(subscription);
        _pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);

        var result = await BuildHandler().Handle(
            new IssueVaccinePassportCommand(
                pet.Id,
                clinic.Id,
                clinicOwnerId,
                Guid.NewGuid(),
                "Dra. Rivera",
                "VET-12345",
                "Dorado",
                [new PassportVaccineEntryInput("Rabia", "Brand", "LOT-1", new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1))],
                null),
            default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("La clínica no está verificada para emitir pasaportes.");
        await _certificates.DidNotReceive().AddAsync(Arg.Any<VetCertificate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_VeterinarianCannotIssueCertificates_ReturnsFailureWithoutIssuingCertificate()
    {
        var clinicOwnerId = Guid.NewGuid();
        var petOwnerId = Guid.NewGuid();
        var revokedBy = Guid.NewGuid();
        var clinic = Clinic.Create(clinicOwnerId, "VetSalud", "SENASA-12345", "Heredia", 10m, -84.1m, "vet@example.com");
        clinic.Activate();
        var verification = MakeVerifiedClinic(clinic);
        var veterinarian = ClinicVeterinarian.Create(clinic.Id, "Dra. Rivera", "VET-12345");
        veterinarian.Revoke(revokedBy, "Ya no labora en la clinica");
        var subscription = Subscription.CreateForClinic(clinic.Id, clinicOwnerId, SubscriptionTier.ClinicPartner, "PASS1234", 35000m);
        subscription.Activate();
        var pet = Pet.Create(petOwnerId, "Nala", PetSpecies.Dog, null, null);

        _subscriptions.GetActiveForClinicAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(subscription);
        _pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        _clinicVerifications.GetActiveForClinicAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(verification);
        _veterinarians.GetByIdAsync(veterinarian.Id, Arg.Any<CancellationToken>()).Returns(veterinarian);
        _grants.HasActiveGrantAsync(clinic.Id, pet.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await BuildHandler().Handle(
            new IssueVaccinePassportCommand(
                pet.Id,
                clinic.Id,
                clinicOwnerId,
                veterinarian.Id,
                "Dra. Rivera",
                "VET-12345",
                "Dorado",
                [new PassportVaccineEntryInput("Rabia", "Brand", "LOT-1", new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1))],
                null),
            default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("El veterinario no está autorizado para emitir pasaportes.");
        await _certificates.DidNotReceive().AddAsync(Arg.Any<VetCertificate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DogWithoutRabiesVaccine_ReturnsFailureWithoutIssuingCertificate()
    {
        var clinicOwnerId = Guid.NewGuid();
        var petOwnerId = Guid.NewGuid();
        var clinic = Clinic.Create(clinicOwnerId, "VetSalud", "SENASA-12345", "Heredia", 10m, -84.1m, "vet@example.com");
        clinic.Activate();
        var verification = MakeVerifiedClinic(clinic);
        var veterinarian = ClinicVeterinarian.Create(clinic.Id, "Dra. Rivera", "VET-12345");
        var subscription = Subscription.CreateForClinic(clinic.Id, clinicOwnerId, SubscriptionTier.ClinicPartner, "PASS1234", 35000m);
        subscription.Activate();
        var pet = Pet.Create(petOwnerId, "Nala", PetSpecies.Dog, null, null);

        _subscriptions.GetActiveForClinicAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(subscription);
        _pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        _clinicVerifications.GetActiveForClinicAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(verification);
        _veterinarians.GetByIdAsync(veterinarian.Id, Arg.Any<CancellationToken>()).Returns(veterinarian);
        _grants.HasActiveGrantAsync(clinic.Id, pet.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await BuildHandler().Handle(
            new IssueVaccinePassportCommand(
                pet.Id,
                clinic.Id,
                clinicOwnerId,
                veterinarian.Id,
                "Dra. Rivera",
                "VET-12345",
                "Dorado",
                [new PassportVaccineEntryInput("Polivalente", "Brand", "LOT-1", new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1))],
                null),
            default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("La vacuna contra la rabia es requerida para perros.");
        await _certificates.DidNotReceive().AddAsync(Arg.Any<VetCertificate>(), Arg.Any<CancellationToken>());
        await _passports.DidNotReceive().AddAsync(Arg.Any<VaccinePassport>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_VeterinarianBelongsToAnotherClinic_ReturnsFailureWithoutIssuingCertificate()
    {
        var clinicOwnerId = Guid.NewGuid();
        var petOwnerId = Guid.NewGuid();
        var clinic = Clinic.Create(clinicOwnerId, "VetSalud", "SENASA-12345", "Heredia", 10m, -84.1m, "vet@example.com");
        clinic.Activate();
        var verification = MakeVerifiedClinic(clinic);
        var veterinarian = ClinicVeterinarian.Create(Guid.NewGuid(), "Dra. Rivera", "VET-12345");
        var subscription = Subscription.CreateForClinic(clinic.Id, clinicOwnerId, SubscriptionTier.ClinicPartner, "PASS1234", 35000m);
        subscription.Activate();
        var pet = Pet.Create(petOwnerId, "Nala", PetSpecies.Dog, null, null);

        _subscriptions.GetActiveForClinicAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(subscription);
        _pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        _clinicVerifications.GetActiveForClinicAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(verification);
        _veterinarians.GetByIdAsync(veterinarian.Id, Arg.Any<CancellationToken>()).Returns(veterinarian);
        _grants.HasActiveGrantAsync(clinic.Id, pet.Id, Arg.Any<CancellationToken>()).Returns(true);

        var result = await BuildHandler().Handle(
            new IssueVaccinePassportCommand(
                pet.Id,
                clinic.Id,
                clinicOwnerId,
                veterinarian.Id,
                "Dra. Rivera",
                "VET-12345",
                "Dorado",
                [new PassportVaccineEntryInput("Rabia", "Brand", "LOT-1", new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1))],
                null),
            default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("El veterinario no está autorizado para emitir pasaportes.");
        await _certificates.DidNotReceive().AddAsync(Arg.Any<VetCertificate>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_PersistsStructuredPassportAndAuditLog()
    {
        var clinicOwnerId = Guid.NewGuid();
        var petOwnerId = Guid.NewGuid();
        var clinic = Clinic.Create(clinicOwnerId, "VetSalud", "SENASA-12345", "Heredia", 10m, -84.1m, "vet@example.com");
        clinic.Activate();
        var verification = MakeVerifiedClinic(clinic);
        var veterinarian = ClinicVeterinarian.Create(clinic.Id, "Dra. Rivera", "VET-12345");
        var subscription = Subscription.CreateForClinic(clinic.Id, clinicOwnerId, SubscriptionTier.ClinicPartner, "PASS1234", 35000m);
        subscription.Activate();
        var pet = Pet.Create(petOwnerId, "Nala", PetSpecies.Dog, null, null);

        _subscriptions.GetActiveForClinicAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(subscription);
        _pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        _clinicVerifications.GetActiveForClinicAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(verification);
        _veterinarians.GetByIdAsync(veterinarian.Id, Arg.Any<CancellationToken>()).Returns(veterinarian);
        _grants.HasActiveGrantAsync(clinic.Id, pet.Id, Arg.Any<CancellationToken>()).Returns(true);
        _certificateService.GenerateAndStoreAsync(Arg.Any<CertificatePdfData>(), Arg.Any<CancellationToken>())
            .Returns("https://storage.example/certificates/passport.pdf");

        var result = await BuildHandler().Handle(
            new IssueVaccinePassportCommand(
                pet.Id,
                clinic.Id,
                clinicOwnerId,
                veterinarian.Id,
                "Dra. Rivera",
                "VET-12345",
                "Dorado",
                [new PassportVaccineEntryInput("Rabia", "Brand", "LOT-1", new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1))],
                null),
            default);

        result.IsSuccess.Should().BeTrue();
        await _certificates.Received(1).AddAsync(Arg.Any<VetCertificate>(), Arg.Any<CancellationToken>());
        await _passports.Received(1).AddAsync(Arg.Any<VaccinePassport>(), Arg.Any<CancellationToken>());
        await _auditLogs.Received(1).AddAsync(
            Arg.Is<CertificateAuditLog>(log => log.Action == CertificateAuditAction.Issued && log.ActorUserId == clinicOwnerId),
            Arg.Any<CancellationToken>());
    }
}

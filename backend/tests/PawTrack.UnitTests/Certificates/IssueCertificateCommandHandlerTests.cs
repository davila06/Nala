using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Certificates.Commands.IssueCertificate;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Pets;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Certificates;

public sealed class IssueCertificateCommandHandlerTests
{
    [Fact]
    public async Task Handle_UsesPersistedPetAndClinicIdentityForPdf()
    {
        var actorId = Guid.NewGuid();
        var clinic = Clinic.Create(actorId, "Clínica verificada", "VET-1234", "San Jose", 9.93m, -84.08m, "vet@example.cr");
        clinic.Activate();
        var pet = Pet.Create(Guid.NewGuid(), "Luna", PetSpecies.Dog, "Criolla", null);
        var clinics = Substitute.For<IClinicRepository>();
        var pets = Substitute.For<IPetRepository>();
        var grants = Substitute.For<IClinicMedicalAccessGrantRepository>();
        var veterinarians = Substitute.For<IClinicVeterinarianRepository>();
        var subscriptions = Substitute.For<ISubscriptionRepository>();
        var certificates = Substitute.For<ICertificateRepository>();
        var certificateService = Substitute.For<ICertificateService>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        grants.HasActiveGrantAsync(clinic.Id, pet.Id, Arg.Any<CancellationToken>()).Returns(true);
        var veterinarian = ClinicVeterinarian.Create(clinic.Id, "Dra. Autorizada", "VET-DR-001");
        veterinarians.GetByIdAsync(veterinarian.Id, Arg.Any<CancellationToken>()).Returns(veterinarian);
        var plan = Subscription.CreateForClinic(clinic.Id, actorId, SubscriptionTier.ClinicPartner, "PDF12345", 35000m);
        plan.Activate();
        subscriptions.GetActiveForClinicAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(plan);
        certificateService.GenerateAndStoreAsync(Arg.Any<CertificatePdfData>(), Arg.Any<CancellationToken>())
            .Returns(new CertificateArtifact("https://test-storage/cert.pdf", null, null));

        var handler = new IssueCertificateCommandHandler(certificates, certificateService, subscriptions,
            clinics, pets, grants, veterinarians, unitOfWork);
        var result = await handler.Handle(new IssueCertificateCommand(pet.Id, clinic.Id, veterinarian.Id, actorId,
            CertificateType.Vaccination, null, null, "Mascota falsa", "Cat", "Otra raza",
            "Clínica falsa", "VET-FALSO", veterinarian.FullName), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await certificateService.Received(1).GenerateAndStoreAsync(Arg.Is<CertificatePdfData>(data =>
            data.PetName == "Luna" && data.PetSpecies == "Dog" && data.PetBreed == "Criolla"
            && data.ClinicName == "Clínica verificada" && data.ClinicLicense == "VET-1234"
            && data.VetName == "Dra. Autorizada" && data.VeterinarianLicense == "VET-DR-001"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CertificateQuotaExhausted_ReturnsFailureBeforeGeneratingArtifact()
    {
        var clinicId = Guid.NewGuid();
        var clinicUserId = Guid.NewGuid();
        var certificates = Substitute.For<ICertificateRepository>();
        var certificateService = Substitute.For<ICertificateService>();
        var subscriptions = Substitute.For<ISubscriptionRepository>();
        var clinics = Substitute.For<IClinicRepository>();
        var pets = Substitute.For<IPetRepository>();
        var grants = Substitute.For<IClinicMedicalAccessGrantRepository>();
        var veterinarians = Substitute.For<IClinicVeterinarianRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clinic = Clinic.Create(clinicUserId, "Vet", "VET-001", "San Jose", 9.93m, -84.08m, "vet@example.cr");
        clinic.Activate();
        var pet = Pet.Create(Guid.NewGuid(), "Max", PetSpecies.Dog, null, null);
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        grants.HasActiveGrantAsync(clinic.Id, pet.Id, Arg.Any<CancellationToken>()).Returns(true);
        var veterinarian = ClinicVeterinarian.Create(clinic.Id, "Dr. Vet", "VET-DR-002");
        veterinarians.GetByIdAsync(veterinarian.Id, Arg.Any<CancellationToken>()).Returns(veterinarian);
        var subscription = Subscription.CreateForClinic(clinic.Id, clinicUserId, SubscriptionTier.ClinicPartner, "PARTNER3", 35_000m);
        subscription.Activate();
        subscriptions.GetActiveForClinicAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(subscription);
        certificates.CountForClinicSinceAsync(clinic.Id, Arg.Any<DateTimeOffset>(), null, Arg.Any<CancellationToken>()).Returns(500);

        var handler = new IssueCertificateCommandHandler(certificates, certificateService, subscriptions,
            clinics, pets, grants, veterinarians, unitOfWork);
        var result = await handler.Handle(new IssueCertificateCommand(
            pet.Id, clinic.Id, veterinarian.Id, clinicUserId, CertificateType.HealthClearance, null, null,
            "Max", "Dog", null, "Vet", "SEN", veterinarian.FullName), default);

        result.IsFailure.Should().BeTrue();
        await certificateService.DidNotReceive().GenerateAndStoreAsync(Arg.Any<CertificatePdfData>(), Arg.Any<CancellationToken>());
        await certificates.DidNotReceive().AddAsync(Arg.Any<VetCertificate>(), Arg.Any<CancellationToken>());
    }
}

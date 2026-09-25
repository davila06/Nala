using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Clinics.Commands.ManageClinicCrm;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Clinics;

public sealed class ClinicCrmSecurityTests
{
    [Fact]
    public async Task ClinicCannotOptInOnBehalfOfOwner()
    {
        var clinicUserId = Guid.NewGuid();
        var clinic = Clinic.Create(clinicUserId, "Clinica Test", "VET-123", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        var pet = Pet.Create(Guid.NewGuid(), "Max", PetSpecies.Dog, null, null);
        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var pets = Substitute.For<IPetRepository>();
        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        var crm = Substitute.For<IClinicCrmRepository>();
        var handler = new UpsertClinicCommunicationPreferenceCommandHandler(clinics, pets, crm, Substitute.For<IAuditLogRepository>(), Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(new UpsertClinicCommunicationPreferenceCommand(clinic.Id, clinicUserId, pet.Id, ClinicCommunicationChannel.WhatsApp, ClinicCommunicationPurpose.Marketing, true, "recepcion"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await crm.DidNotReceive().AddPreferenceAsync(Arg.Any<ClinicClientCommunicationPreference>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClinicCannotLogActivityForUnrelatedPet()
    {
        var clinicUserId = Guid.NewGuid();
        var clinic = Clinic.Create(clinicUserId, "Clinica Test", "VET-123", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        var pet = Pet.Create(Guid.NewGuid(), "Max", PetSpecies.Dog, null, null);
        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var pets = Substitute.For<IPetRepository>();
        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        var crm = Substitute.For<IClinicCrmRepository>();
        var handler = new LogClinicCommunicationActivityCommandHandler(clinics, pets, crm, Substitute.For<IAuditLogRepository>(), Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(new LogClinicCommunicationActivityCommand(clinic.Id, clinicUserId, pet.Id, ClinicCommunicationChannel.Email, ClinicCommunicationPurpose.ClinicalFollowUp, ClinicCommunicationDirection.Outbound, ClinicCommunicationStatus.LoggedExternally, "Seguimiento", "Contacto", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await crm.DidNotReceive().AddActivityAsync(Arg.Any<ClinicClientCommunicationActivity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OwnerCannotGrantConsentForSomeoneElsesPet()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "Clinica Test", "VET-123", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        var pet = Pet.Create(Guid.NewGuid(), "Max", PetSpecies.Dog, null, null);
        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var pets = Substitute.For<IPetRepository>();
        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        var crm = Substitute.For<IClinicCrmRepository>();
        crm.HasClinicPatientRelationshipAsync(clinic.Id, pet.Id, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new SetOwnerClinicCommunicationPreferenceCommandHandler(clinics, pets, crm, Substitute.For<IAuditLogRepository>(), Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(new SetOwnerClinicCommunicationPreferenceCommand(clinic.Id, Guid.NewGuid(), pet.Id, ClinicCommunicationChannel.Email, ClinicCommunicationPurpose.ClinicalFollowUp, true), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await crm.DidNotReceive().AddPreferenceAsync(Arg.Any<ClinicClientCommunicationPreference>(), Arg.Any<CancellationToken>());
    }
}

using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Clinics.Commands.ManageClinicCrm;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Pets;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Common;

namespace PawTrack.UnitTests.Clinics;

public sealed class ClinicCommunicationSendTests
{
    [Fact]
    public async Task ConsentAndVerifiedOwner_QueuesBeforeSendAndRecordsProviderReceipt()
    {
        var userId = Guid.NewGuid();
        var clinic = Clinic.Create(userId, "Clinica", "LIC-1", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        var (owner, token) = User.Create("owner@pawtrack.cr", "hash", "Owner");
        owner.VerifyEmail(token);
        var pet = Pet.Create(owner.Id, "Max", PetSpecies.Dog, null, null);
        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var pets = Substitute.For<IPetRepository>();
        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(owner);
        var crm = Substitute.For<IClinicCrmRepository>();
        crm.HasClinicPatientRelationshipAsync(clinic.Id, pet.Id, Arg.Any<CancellationToken>()).Returns(true);
        crm.GetPreferenceAsync(clinic.Id, pet.Id, ClinicCommunicationChannel.Email, ClinicCommunicationPurpose.ClinicalFollowUp, Arg.Any<CancellationToken>())
            .Returns(ClinicClientCommunicationPreference.Create(clinic.Id, pet.Id, owner.Id, ClinicCommunicationChannel.Email, ClinicCommunicationPurpose.ClinicalFollowUp, true, "Portal", owner.Id));
        ClinicClientCommunicationActivity? activity = null;
        crm.AddActivityAsync(Arg.Do<ClinicClientCommunicationActivity>(value => activity = value), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var gateway = Substitute.For<IClinicEmailGateway>();
        gateway.SendAsync(owner.Email, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                activity!.Status.Should().Be(ClinicCommunicationStatus.Queued);
                return Task.FromResult(Result.Success("sg-message-123"));
            });
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new SendClinicCommunicationTemplateCommandHandler(clinics, pets, users, crm, gateway,
            Substitute.For<IAuditLogRepository>(), unitOfWork);

        var result = await handler.Handle(new SendClinicCommunicationTemplateCommand(clinic.Id, userId, pet.Id,
            Guid.NewGuid(), "clinical-follow-up", ClinicCommunicationChannel.Email), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        activity!.Status.Should().Be(ClinicCommunicationStatus.Sent);
        activity.ProviderMessageId.Should().Be("sg-message-123");
        await unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MissingPurposeSpecificConsent_DoesNotContactProvider()
    {
        var userId = Guid.NewGuid();
        var clinic = Clinic.Create(userId, "Clinica", "LIC-1", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        var pet = Pet.Create(Guid.NewGuid(), "Max", PetSpecies.Dog, null, null);
        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var pets = Substitute.For<IPetRepository>();
        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        var crm = Substitute.For<IClinicCrmRepository>();
        crm.HasClinicPatientRelationshipAsync(clinic.Id, pet.Id, Arg.Any<CancellationToken>()).Returns(true);
        var gateway = Substitute.For<IClinicEmailGateway>();
        var handler = new SendClinicCommunicationTemplateCommandHandler(clinics, pets,
            Substitute.For<IUserRepository>(), crm, gateway, Substitute.For<IAuditLogRepository>(), Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(new SendClinicCommunicationTemplateCommand(clinic.Id, userId, pet.Id,
            Guid.NewGuid(), "clinical-follow-up", ClinicCommunicationChannel.Email), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await gateway.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await crm.DidNotReceive().AddActivityAsync(Arg.Any<ClinicClientCommunicationActivity>(), Arg.Any<CancellationToken>());
    }
}

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
    public async Task NonOwnerCannotOptOutThroughOwnerOnlyCommand()
    {
        var ownerId = Guid.NewGuid();
        var clinic = Clinic.Create(ownerId, "Clinica Test", "VET-123", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        var pet = Pet.Create(Guid.NewGuid(), "Max", PetSpecies.Dog, null, null);
        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var pets = Substitute.For<IPetRepository>();
        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        var crm = Substitute.For<IClinicCrmRepository>();
        crm.HasClinicPatientRelationshipAsync(clinic.Id, pet.Id, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new UpsertClinicCommunicationPreferenceCommandHandler(clinics, pets, crm, Substitute.For<IAuditLogRepository>(), Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(new UpsertClinicCommunicationPreferenceCommand(clinic.Id, Guid.NewGuid(), pet.Id, ClinicCommunicationChannel.Email, ClinicCommunicationPurpose.ClinicalFollowUp, false, "Tutor lo solicitó"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await crm.DidNotReceive().AddPreferenceAsync(Arg.Any<ClinicClientCommunicationPreference>(), Arg.Any<CancellationToken>());
        crm.DidNotReceive().UpdatePreference(Arg.Any<ClinicClientCommunicationPreference>());
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

    [Fact]
    public async Task ReceptionistDashboardContainsOnlyReceptionTasksAndNoCrmContactData()
    {
        var ownerId = Guid.NewGuid();
        var staffUserId = Guid.NewGuid();
        var clinic = Clinic.Create(ownerId, "Clinica Test", "VET-123", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        var membership = ClinicStaffMembership.Grant(clinic.Id, staffUserId, ClinicStaffRole.Receptionist, ownerId);
        var clinicRepository = Substitute.For<IClinicRepository>();
        clinicRepository.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var staffAccess = Substitute.For<IClinicStaffAccessRepository>();
        staffAccess.GetAsync(clinic.Id, staffUserId, Arg.Any<CancellationToken>()).Returns(membership);
        var crm = Substitute.For<IClinicCrmRepository>();
        crm.GetDashboardAsync(clinic.Id, new DateOnly(2026, 9, 25), Arg.Any<IReadOnlyCollection<ClinicInternalTaskRole>>(), Arg.Any<IReadOnlyCollection<ClinicCrmTaskType>>(), Arg.Any<bool>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(
            new ClinicCrmDashboardReadModel(
                [],
                [],
                [
                    new ClinicCrmTaskReadModel(Guid.NewGuid(), Guid.NewGuid(), "Nala", Guid.NewGuid(), "Tutor", ClinicCrmTaskType.ConfirmAppointment, ClinicInternalTaskRole.Receptionist, null, null, ClinicCrmTaskPriority.Normal, ClinicCrmTaskStatus.Open, new DateOnly(2026, 9, 25), "Confirmar cita", null),
                ],
                []));
        var handler = new GetClinicCrmDashboardQueryHandler(clinicRepository, staffAccess,
            Substitute.For<IClinicFinanceAccessRepository>(), crm);

        var result = await handler.Handle(new GetClinicCrmDashboardQuery(clinic.Id, staffUserId, new DateOnly(2026, 9, 25)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OpenTasks.Should().ContainSingle(task => task.Type == nameof(ClinicCrmTaskType.ConfirmAppointment));
        result.Value.RecentActivities.Should().BeEmpty();
        result.Value.Preferences.Should().BeEmpty();
        result.Value.Segments.Should().BeEmpty();
    }

    [Fact]
    public async Task ReceptionistCannotCreateVeterinarianTask()
    {
        var ownerId = Guid.NewGuid();
        var staffUserId = Guid.NewGuid();
        var clinic = Clinic.Create(ownerId, "Clinica Test", "VET-123", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        var pet = Pet.Create(Guid.NewGuid(), "Max", PetSpecies.Dog, null, null);
        var membership = ClinicStaffMembership.Grant(clinic.Id, staffUserId, ClinicStaffRole.Receptionist, ownerId);
        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var pets = Substitute.For<IPetRepository>();
        pets.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        var staffAccess = Substitute.For<IClinicStaffAccessRepository>();
        staffAccess.GetAsync(clinic.Id, staffUserId, Arg.Any<CancellationToken>()).Returns(membership);
        var crm = Substitute.For<IClinicCrmRepository>();
        crm.HasClinicPatientRelationshipAsync(clinic.Id, pet.Id, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new CreateClinicCrmTaskCommandHandler(clinics, pets, staffAccess,
            Substitute.For<IClinicFinanceAccessRepository>(), crm, Substitute.For<IAuditLogRepository>(), Substitute.For<IUnitOfWork>());

        var result = await handler.Handle(new CreateClinicCrmTaskCommand(clinic.Id, staffUserId, pet.Id,
            ClinicCrmTaskType.FollowUpTreatment, new DateOnly(2026, 9, 25), "Seguimiento", null, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await crm.DidNotReceive().AddTaskAsync(Arg.Any<ClinicCrmTask>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConcurrentIdempotencyReplay_ReturnsTheExistingMatchingTask()
    {
        var ownerId = Guid.NewGuid();
        var clinic = Clinic.Create(ownerId, "Clinica Test", "VET-123", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        var idempotencyKey = Guid.NewGuid();
        var existing = ClinicCrmTask.Create(clinic.Id, null, null, ClinicCrmTaskType.CloseCash,
            new DateOnly(2026, 9, 25), "Cerrar caja", null, ownerId, ClinicInternalTaskRole.Manager,
            ClinicCrmTaskPriority.High, ownerId, idempotencyKey);
        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var crm = Substitute.For<IClinicCrmRepository>();
        crm.GetTaskByIdempotencyKeyAsync(clinic.Id, idempotencyKey, Arg.Any<CancellationToken>())
            .Returns((ClinicCrmTask?)null, existing);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var saveFailure = Task.FromException<int>(new InvalidOperationException("Unique key race"));
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(saveFailure);
        var handler = new CreateClinicCrmTaskCommandHandler(clinics, Substitute.For<IPetRepository>(),
            Substitute.For<IClinicStaffAccessRepository>(), Substitute.For<IClinicFinanceAccessRepository>(), crm,
            Substitute.For<IAuditLogRepository>(), unitOfWork);

        var result = await handler.Handle(new CreateClinicCrmTaskCommand(clinic.Id, ownerId, null,
            ClinicCrmTaskType.CloseCash, new DateOnly(2026, 9, 25), "Cerrar caja", null, idempotencyKey,
            ClinicCrmTaskPriority.High, ClinicInternalTaskRole.Manager, ownerId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(existing.Id);
        crm.Received(1).DetachTask(Arg.Any<ClinicCrmTask>());
    }
}

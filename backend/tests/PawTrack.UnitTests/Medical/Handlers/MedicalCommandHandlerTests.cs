using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Medical;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Medical;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Medical.Handlers;

// ── Shared helpers ────────────────────────────────────────────────────────────

file static class MedicalTestHelpers
{
    public static MedicalRecord CreateOwnerRecord(
        Guid petId, Guid createdByUserId, string? documentUrl = null)
    {
        var r = MedicalRecord.Create(petId, createdByUserId, MedicalRecordType.Checkup,
            new DateOnly(2026, 1, 1), "Annual checkup", "Dr. Smith", "PawClinic", null);
        if (documentUrl is not null) r.SetDocumentUrl(documentUrl);
        return r;
    }

    public static VetReminder CreateReminder(Guid petId, Guid ownerId) =>
        VetReminder.Create(petId, ownerId, MedicalRecordType.Vaccine,
            new DateOnly(2026, 12, 1), "Rabies booster", null);
}

// ═════════════════════════════════════════════════════════════════════════════
// DeleteMedicalRecordCommandHandlerTests
// ═════════════════════════════════════════════════════════════════════════════

public sealed class DeleteMedicalRecordCommandHandlerTests
{
    private readonly IPetRepository _petRepo = Substitute.For<IPetRepository>();
    private readonly IMedicalRepository _medRepo = Substitute.For<IMedicalRepository>();
    private readonly IFamilyRepository _familyRepo = Substitute.For<IFamilyRepository>();
    private readonly ISubscriptionService _subs = Substitute.For<ISubscriptionService>();
    private readonly IBlobStorageService _blob = Substitute.For<IBlobStorageService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly DeleteMedicalRecordCommandHandler _sut;

    public DeleteMedicalRecordCommandHandlerTests()
    {
        _subs.IsFamiliaAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        _sut = new DeleteMedicalRecordCommandHandler(_petRepo, _medRepo, _familyRepo, _subs, _blob, _uow);
    }

    [Fact]
    public async Task Handle_CreatorDeletes_SucceedsAndCallsRepoDelete()
    {
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Max", PetSpecies.Dog, null, null);
        var record = MedicalTestHelpers.CreateOwnerRecord(pet.Id, ownerId);

        _medRepo.GetByIdAsync(record.Id, Arg.Any<CancellationToken>()).Returns(record);
        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);

        var result = await _sut.Handle(new DeleteMedicalRecordCommand(record.Id, ownerId), default);

        result.IsSuccess.Should().BeTrue();
        _medRepo.Received(1).Delete(record);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PetOwnerNotCreator_CanStillDelete()
    {
        var ownerId = Guid.NewGuid();
        var vetUserId = Guid.NewGuid();   // clinic user created this record
        var pet = Pet.Create(ownerId, "Luna", PetSpecies.Cat, null, null);
        var record = MedicalTestHelpers.CreateOwnerRecord(pet.Id, vetUserId);

        _medRepo.GetByIdAsync(record.Id, Arg.Any<CancellationToken>()).Returns(record);
        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);

        var result = await _sut.Handle(new DeleteMedicalRecordCommand(record.Id, ownerId), default);

        result.IsSuccess.Should().BeTrue();
        _medRepo.Received(1).Delete(record);
    }

    [Fact]
    public async Task Handle_FamilyMemberDeletes_Succeeds()
    {
        var ownerId = Guid.NewGuid();
        var familyMemberId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Toby", PetSpecies.Dog, null, null);
        var record = MedicalTestHelpers.CreateOwnerRecord(pet.Id, ownerId);

        _medRepo.GetByIdAsync(record.Id, Arg.Any<CancellationToken>()).Returns(record);
        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _familyRepo.GetActiveMemberIdsAsync(ownerId, Arg.Any<CancellationToken>())
                   .Returns(new List<Guid> { familyMemberId });

        var result = await _sut.Handle(new DeleteMedicalRecordCommand(record.Id, familyMemberId), default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UnrelatedUser_ReturnsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var stranger = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Bruno", PetSpecies.Dog, null, null);
        var record = MedicalTestHelpers.CreateOwnerRecord(pet.Id, ownerId);

        _medRepo.GetByIdAsync(record.Id, Arg.Any<CancellationToken>()).Returns(record);
        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _familyRepo.GetActiveMemberIdsAsync(ownerId, Arg.Any<CancellationToken>())
                   .Returns(new List<Guid>());

        var result = await _sut.Handle(new DeleteMedicalRecordCommand(record.Id, stranger), default);

        result.IsFailure.Should().BeTrue();
        _medRepo.DidNotReceive().Delete(Arg.Any<MedicalRecord>());
    }

    [Fact]
    public async Task Handle_RecordHasDocument_DeletesBlobBeforeRecord()
    {
        var ownerId = Guid.NewGuid();
        const string blobUrl = "https://storage.blob.core.windows.net/medical-docs/pet/rec.pdf";
        var pet = Pet.Create(ownerId, "Coco", PetSpecies.Dog, null, null);
        var record = MedicalTestHelpers.CreateOwnerRecord(pet.Id, ownerId, blobUrl);

        _medRepo.GetByIdAsync(record.Id, Arg.Any<CancellationToken>()).Returns(record);
        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);

        var result = await _sut.Handle(new DeleteMedicalRecordCommand(record.Id, ownerId), default);

        result.IsSuccess.Should().BeTrue();
        await _blob.Received(1).DeleteAsync(blobUrl, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RecordNoDocument_NoBlobCall()
    {
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Paco", PetSpecies.Dog, null, null);
        var record = MedicalTestHelpers.CreateOwnerRecord(pet.Id, ownerId);

        _medRepo.GetByIdAsync(record.Id, Arg.Any<CancellationToken>()).Returns(record);
        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);

        await _sut.Handle(new DeleteMedicalRecordCommand(record.Id, ownerId), default);

        await _blob.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RecordNotFound_ReturnsFailure()
    {
        _medRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns((MedicalRecord?)null);

        var result = await _sut.Handle(new DeleteMedicalRecordCommand(Guid.NewGuid(), Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NoFamiliaPlan_ReturnsFailure()
    {
        _subs.IsFamiliaAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.Handle(new DeleteMedicalRecordCommand(Guid.NewGuid(), Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().Contain("Familia");
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// UpdateMedicalRecordCommandHandlerTests
// ═════════════════════════════════════════════════════════════════════════════

public sealed class UpdateMedicalRecordCommandHandlerTests
{
    private readonly IPetRepository _petRepo = Substitute.For<IPetRepository>();
    private readonly IMedicalRepository _medRepo = Substitute.For<IMedicalRepository>();
    private readonly IFamilyRepository _familyRepo = Substitute.For<IFamilyRepository>();
    private readonly ISubscriptionService _subs = Substitute.For<ISubscriptionService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly UpdateMedicalRecordCommandHandler _sut;

    public UpdateMedicalRecordCommandHandlerTests()
    {
        _subs.IsFamiliaAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        _sut = new UpdateMedicalRecordCommandHandler(_petRepo, _medRepo, _familyRepo, _subs, _uow);
    }

    private static UpdateMedicalRecordCommand BuildUpdateCmd(Guid recordId, Guid userId) =>
        new(recordId, userId, MedicalRecordType.Checkup,
            new DateOnly(2026, 6, 1), "Updated description",
            "Dr. Updated", "New Clinic", null);

    [Fact]
    public async Task Handle_CreatorUpdates_ReturnsUpdatedDto()
    {
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Rex", PetSpecies.Dog, null, null);
        var record = MedicalTestHelpers.CreateOwnerRecord(pet.Id, ownerId);

        _medRepo.GetByIdAsync(record.Id, Arg.Any<CancellationToken>()).Returns(record);
        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);

        var result = await _sut.Handle(BuildUpdateCmd(record.Id, ownerId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().Be("Updated description");
        _medRepo.Received(1).Update(record);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FamilyMemberEdits_Succeeds()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Mimi", PetSpecies.Cat, null, null);
        var record = MedicalTestHelpers.CreateOwnerRecord(pet.Id, ownerId);

        _medRepo.GetByIdAsync(record.Id, Arg.Any<CancellationToken>()).Returns(record);
        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _familyRepo.GetActiveMemberIdsAsync(ownerId, Arg.Any<CancellationToken>())
                   .Returns(new List<Guid> { memberId });

        var result = await _sut.Handle(BuildUpdateCmd(record.Id, memberId), default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NonCreatorNonFamily_ReturnsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var stranger = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Nala", PetSpecies.Dog, null, null);
        var record = MedicalTestHelpers.CreateOwnerRecord(pet.Id, ownerId);

        _medRepo.GetByIdAsync(record.Id, Arg.Any<CancellationToken>()).Returns(record);
        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _familyRepo.GetActiveMemberIdsAsync(ownerId, Arg.Any<CancellationToken>())
                   .Returns(new List<Guid>());

        var result = await _sut.Handle(BuildUpdateCmd(record.Id, stranger), default);

        result.IsFailure.Should().BeTrue();
        _medRepo.DidNotReceive().Update(Arg.Any<MedicalRecord>());
    }

    [Fact]
    public async Task Handle_RecordNotFound_ReturnsFailure()
    {
        _medRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns((MedicalRecord?)null);

        var result = await _sut.Handle(BuildUpdateCmd(Guid.NewGuid(), Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// CreateVetReminderCommandHandlerTests
// ═════════════════════════════════════════════════════════════════════════════

public sealed class CreateVetReminderCommandHandlerTests
{
    private readonly IPetRepository _petRepo = Substitute.For<IPetRepository>();
    private readonly IMedicalRepository _medRepo = Substitute.For<IMedicalRepository>();
    private readonly IFamilyRepository _familyRepo = Substitute.For<IFamilyRepository>();
    private readonly ISubscriptionService _subs = Substitute.For<ISubscriptionService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly CreateVetReminderCommandHandler _sut;

    public CreateVetReminderCommandHandlerTests()
    {
        _subs.IsFamiliaAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        _sut = new CreateVetReminderCommandHandler(_petRepo, _medRepo, _familyRepo, _subs, _uow);
    }

    [Fact]
    public async Task Handle_ValidPayload_CreatesReminderAndReturnsDto()
    {
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Bolt", PetSpecies.Dog, null, null);
        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _familyRepo.GetActiveMemberIdsAsync(ownerId, Arg.Any<CancellationToken>())
                   .Returns(new List<Guid>());

        var cmd = new CreateVetReminderCommand(
            pet.Id, ownerId, MedicalRecordType.Vaccine,
            new DateOnly(2027, 1, 15), "Annual rabies vaccine", "Bring previous card");

        var result = await _sut.Handle(cmd, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Annual rabies vaccine");
        result.Value.IsCompleted.Should().BeFalse();
        await _medRepo.Received(1).AddReminderAsync(Arg.Any<VetReminder>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PetNotFound_ReturnsFailure()
    {
        _petRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns((Pet?)null);

        var cmd = new CreateVetReminderCommand(Guid.NewGuid(), Guid.NewGuid(),
            MedicalRecordType.Vaccine, new DateOnly(2027, 1, 1), "title", null);

        var result = await _sut.Handle(cmd, default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().Contain("no encontrada");
    }

    [Fact]
    public async Task Handle_NoFamiliaPlan_ReturnsFailure()
    {
        _subs.IsFamiliaAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var cmd = new CreateVetReminderCommand(Guid.NewGuid(), Guid.NewGuid(),
            MedicalRecordType.Vaccine, new DateOnly(2027, 1, 1), "title", null);

        var result = await _sut.Handle(cmd, default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Should().Contain("Familia");
    }

    [Fact]
    public async Task Handle_UnrelatedUser_ReturnsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var stranger = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Coco", PetSpecies.Dog, null, null);

        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _familyRepo.GetActiveMemberIdsAsync(ownerId, Arg.Any<CancellationToken>())
                   .Returns(new List<Guid>());

        var cmd = new CreateVetReminderCommand(pet.Id, stranger,
            MedicalRecordType.Vaccine, new DateOnly(2027, 1, 1), "title", null);

        var result = await _sut.Handle(cmd, default);

        result.IsFailure.Should().BeTrue();
        await _medRepo.DidNotReceive().AddReminderAsync(Arg.Any<VetReminder>(), Arg.Any<CancellationToken>());
    }
}

// ═════════════════════════════════════════════════════════════════════════════
// DeleteVetReminderCommandHandlerTests
// ═════════════════════════════════════════════════════════════════════════════

public sealed class DeleteVetReminderCommandHandlerTests
{
    private readonly IPetRepository _petRepo = Substitute.For<IPetRepository>();
    private readonly IMedicalRepository _medRepo = Substitute.For<IMedicalRepository>();
    private readonly IFamilyRepository _familyRepo = Substitute.For<IFamilyRepository>();
    private readonly ISubscriptionService _subs = Substitute.For<ISubscriptionService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly DeleteVetReminderCommandHandler _sut;

    public DeleteVetReminderCommandHandlerTests()
    {
        _subs.IsFamiliaAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        _sut = new DeleteVetReminderCommandHandler(_petRepo, _medRepo, _familyRepo, _subs, _uow);
    }

    [Fact]
    public async Task Handle_OwnerDeletesOwnReminder_Succeeds()
    {
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Skip", PetSpecies.Dog, null, null);
        var reminder = MedicalTestHelpers.CreateReminder(pet.Id, ownerId);

        _medRepo.GetReminderByIdAsync(reminder.Id, Arg.Any<CancellationToken>()).Returns(reminder);
        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);

        var result = await _sut.Handle(new DeleteVetReminderCommand(reminder.Id, ownerId), default);

        result.IsSuccess.Should().BeTrue();
        _medRepo.Received(1).DeleteReminder(reminder);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FamilyMemberDeletes_Succeeds()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Lola", PetSpecies.Cat, null, null);
        var reminder = MedicalTestHelpers.CreateReminder(pet.Id, ownerId);

        _medRepo.GetReminderByIdAsync(reminder.Id, Arg.Any<CancellationToken>()).Returns(reminder);
        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _familyRepo.GetActiveMemberIdsAsync(ownerId, Arg.Any<CancellationToken>())
                   .Returns(new List<Guid> { memberId });

        var result = await _sut.Handle(new DeleteVetReminderCommand(reminder.Id, memberId), default);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UnrelatedUserDeletes_ReturnsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var stranger = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Pepe", PetSpecies.Dog, null, null);
        var reminder = MedicalTestHelpers.CreateReminder(pet.Id, ownerId);

        _medRepo.GetReminderByIdAsync(reminder.Id, Arg.Any<CancellationToken>()).Returns(reminder);
        _petRepo.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _familyRepo.GetActiveMemberIdsAsync(ownerId, Arg.Any<CancellationToken>())
                   .Returns(new List<Guid>());

        var result = await _sut.Handle(new DeleteVetReminderCommand(reminder.Id, stranger), default);

        result.IsFailure.Should().BeTrue();
        _medRepo.DidNotReceive().DeleteReminder(Arg.Any<VetReminder>());
    }

    [Fact]
    public async Task Handle_ReminderNotFound_ReturnsFailure()
    {
        _medRepo.GetReminderByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                .Returns((VetReminder?)null);

        var result = await _sut.Handle(new DeleteVetReminderCommand(Guid.NewGuid(), Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NoFamiliaPlan_ReturnsFailure()
    {
        _subs.IsFamiliaAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.Handle(new DeleteVetReminderCommand(Guid.NewGuid(), Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
    }
}

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PawTrack.Application.Collars;
using PawTrack.Application.Collars.Commands.ActivateCollarTag;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Collars;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Collars.Handlers;

public sealed class ActivateCollarTagCommandHandlerTests
{
    private readonly ICollarTagRepository _tagRepo = Substitute.For<ICollarTagRepository>();
    private readonly ICollarDeviceCredentialRepository _credRepo = Substitute.For<ICollarDeviceCredentialRepository>();
    private readonly ICollarRepository _collarRepo = Substitute.For<ICollarRepository>();
    private readonly ICollarAuditRepository _auditRepo = Substitute.For<ICollarAuditRepository>();
    private readonly IPetRepository _petRepo = Substitute.For<IPetRepository>();
    private readonly ISubscriptionService _subs = Substitute.For<ISubscriptionService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly ActivateCollarTagCommandHandler _sut;

    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid PetId = Guid.NewGuid();
    private const string ValidSerial = "PT-A3F9-0001234";

    public ActivateCollarTagCommandHandlerTests()
    {
        _sut = new ActivateCollarTagCommandHandler(
            _tagRepo, _credRepo, _collarRepo, _auditRepo, _petRepo, _subs, _uow);
    }

    private static CollarTag MakeTag(string serial = ValidSerial)
    {
        var tag = CollarTag.CreateFromFactory(serial, "1.0.0");
        return tag;
    }

    private static Pet MakePet(Guid petId, Guid ownerId)
    {
        var pet = Pet.Create(ownerId, "Firulais", PetSpecies.Dog, null, null);
        typeof(Pet).GetProperty("Id")!.SetValue(pet, petId);
        typeof(Pet).GetProperty("OwnerId")!.SetValue(pet, ownerId);
        return pet;
    }

    [Fact]
    public async Task Handle_HappyPath_ReturnsCollarIdAndRawKey()
    {
        var tag = MakeTag();
        var pet = MakePet(PetId, OwnerId);
        _tagRepo.GetBySerialAsync(ValidSerial, Arg.Any<CancellationToken>()).Returns(tag);
        _petRepo.GetByIdAsync(PetId, Arg.Any<CancellationToken>()).Returns(pet);
        _subs.IsAtLeastPlusAsync(OwnerId, Arg.Any<CancellationToken>()).Returns(true);
        _collarRepo.GetActiveForPetAsync(PetId, Arg.Any<CancellationToken>()).Returns((Collar?)null);
        _credRepo.GetForCollarAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CollarDeviceCredential>() as IReadOnlyList<CollarDeviceCredential>);

        var result = await _sut.Handle(new ActivateCollarTagCommand(ValidSerial, PetId, OwnerId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.CollarApiKey.Should().StartWith("ptwk_collar_");
        result.Value.Serial.Should().Be(ValidSerial);
        tag.Status.Should().Be(CollarTagStatus.Activated);
        await _uow.Received(1).SaveChangesAsync(default);
        await _auditRepo.Received(1).AddAsync(
            Arg.Is<PawTrack.Domain.Collars.CollarAuditEntry>(e => e.Event == PawTrack.Domain.Collars.CollarAuditEvent.Activated),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SerialNotFound_ReturnsFailure()
    {
        _tagRepo.GetBySerialAsync(ValidSerial, Arg.Any<CancellationToken>()).Returns((CollarTag?)null);

        var result = await _sut.Handle(new ActivateCollarTagCommand(ValidSerial, PetId, OwnerId), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainMatch("*no encontrado*");
    }

    [Fact]
    public async Task Handle_SerialAlreadyActivated_ReturnsFailure()
    {
        var tag = MakeTag();
        tag.Activate(Guid.NewGuid());
        _tagRepo.GetBySerialAsync(ValidSerial, Arg.Any<CancellationToken>()).Returns(tag);

        var result = await _sut.Handle(new ActivateCollarTagCommand(ValidSerial, PetId, OwnerId), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainMatch("*Activated*");
    }

    [Fact]
    public async Task Handle_PetBelongsToOtherUser_ReturnsAccessDenied()
    {
        var tag = MakeTag();
        var pet = MakePet(PetId, Guid.NewGuid()); // different owner
        _tagRepo.GetBySerialAsync(ValidSerial, Arg.Any<CancellationToken>()).Returns(tag);
        _petRepo.GetByIdAsync(PetId, Arg.Any<CancellationToken>()).Returns(pet);

        var result = await _sut.Handle(new ActivateCollarTagCommand(ValidSerial, PetId, OwnerId), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Access denied.");
    }

    [Fact]
    public async Task Handle_NoPlusPlan_ReturnsFailure()
    {
        var tag = MakeTag();
        var pet = MakePet(PetId, OwnerId);
        _tagRepo.GetBySerialAsync(ValidSerial, Arg.Any<CancellationToken>()).Returns(tag);
        _petRepo.GetByIdAsync(PetId, Arg.Any<CancellationToken>()).Returns(pet);
        _subs.IsAtLeastPlusAsync(OwnerId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.Handle(new ActivateCollarTagCommand(ValidSerial, PetId, OwnerId), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainMatch("*Plus*");
    }
}

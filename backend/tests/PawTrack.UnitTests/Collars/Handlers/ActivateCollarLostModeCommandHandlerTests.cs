using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Collars.Commands.ActivateCollarLostMode;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;
using PawTrack.Domain.LostPets;

namespace PawTrack.UnitTests.Collars.Handlers;

public sealed class ActivateCollarLostModeCommandHandlerTests
{
    private readonly ICollarRepository _collarRepo = Substitute.For<ICollarRepository>();
    private readonly ILostPetRepository _lostPetRepo = Substitute.For<ILostPetRepository>();
    private readonly ICollarAuditRepository _auditRepo = Substitute.For<ICollarAuditRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly ActivateCollarLostModeCommandHandler _sut;
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid CollarId = Guid.NewGuid();
    private static readonly Guid PetId = Guid.NewGuid();

    public ActivateCollarLostModeCommandHandlerTests()
    {
        _sut = new ActivateCollarLostModeCommandHandler(_collarRepo, _lostPetRepo, _auditRepo, _uow);
    }

    private static Collar MakeCollar()
    {
        var collar = Collar.Register(PetId, OwnerId, CollarProvider.Own, null);
        typeof(Collar).GetProperty("Id")!.SetValue(collar, CollarId);
        return collar;
    }

    [Fact]
    public async Task Handle_NoExistingReport_CreatesNewLostPetEventAndActivatesLostMode()
    {
        var collar = MakeCollar();
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);
        _lostPetRepo.GetActiveByPetIdAsync(PetId, Arg.Any<CancellationToken>()).Returns((LostPetEvent?)null);

        var result = await _sut.Handle(new ActivateCollarLostModeCommand(CollarId, OwnerId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WasNewlyCreated.Should().BeTrue();
        collar.IsLost.Should().BeTrue();
        await _lostPetRepo.Received(1).AddAsync(Arg.Any<LostPetEvent>(), Arg.Any<CancellationToken>());
        await _auditRepo.Received(1).AddAsync(
            Arg.Is<CollarAuditEntry>(e => e.Event == CollarAuditEvent.LostModeActivated),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingActiveReport_ReusesItInsteadOfCreatingNew()
    {
        var collar = MakeCollar();
        var existingEvent = LostPetEvent.Create(PetId, OwnerId, null, null, null, DateTimeOffset.UtcNow);
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);
        _lostPetRepo.GetActiveByPetIdAsync(PetId, Arg.Any<CancellationToken>()).Returns(existingEvent);

        var result = await _sut.Handle(new ActivateCollarLostModeCommand(CollarId, OwnerId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WasNewlyCreated.Should().BeFalse();
        result.Value.LostPetEventId.Should().Be(existingEvent.Id);
        await _lostPetRepo.DidNotReceive().AddAsync(Arg.Any<LostPetEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyLost_ReturnsFailure()
    {
        var collar = MakeCollar();
        collar.ActivateLostMode(Guid.NewGuid());
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);

        var result = await _sut.Handle(new ActivateCollarLostModeCommand(CollarId, OwnerId), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainMatch("*ya está activo*");
    }

    [Fact]
    public async Task Handle_WrongOwner_ReturnsAccessDenied()
    {
        var collar = Collar.Register(PetId, Guid.NewGuid(), CollarProvider.Own, null);
        typeof(Collar).GetProperty("Id")!.SetValue(collar, CollarId);
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);

        var result = await _sut.Handle(new ActivateCollarLostModeCommand(CollarId, OwnerId), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Access denied.");
    }
}

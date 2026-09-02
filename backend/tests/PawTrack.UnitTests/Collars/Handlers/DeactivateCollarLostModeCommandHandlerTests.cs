using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Collars.Commands.DeactivateCollarLostMode;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;

namespace PawTrack.UnitTests.Collars.Handlers;

public sealed class DeactivateCollarLostModeCommandHandlerTests
{
    private readonly ICollarRepository _collarRepo = Substitute.For<ICollarRepository>();
    private readonly ICollarAuditRepository _auditRepo = Substitute.For<ICollarAuditRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly DeactivateCollarLostModeCommandHandler _sut;
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid CollarId = Guid.NewGuid();

    public DeactivateCollarLostModeCommandHandlerTests()
    {
        _sut = new DeactivateCollarLostModeCommandHandler(_collarRepo, _auditRepo, _uow);
    }

    private static Collar MakeLostCollar()
    {
        var collar = Collar.Register(Guid.NewGuid(), OwnerId, CollarProvider.Own, null);
        typeof(Collar).GetProperty("Id")!.SetValue(collar, CollarId);
        collar.ActivateLostMode(Guid.NewGuid());
        return collar;
    }

    [Fact]
    public async Task Handle_HappyPath_DeactivatesLostMode()
    {
        var collar = MakeLostCollar();
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);

        var result = await _sut.Handle(new DeactivateCollarLostModeCommand(CollarId, OwnerId, "Encontrado"), default);

        result.IsSuccess.Should().BeTrue();
        collar.IsLost.Should().BeFalse();
        await _auditRepo.Received(1).AddAsync(
            Arg.Is<CollarAuditEntry>(e => e.Event == CollarAuditEvent.LostModeDeactivated && e.Details == "Encontrado"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotLost_ReturnsFailure()
    {
        var collar = Collar.Register(Guid.NewGuid(), OwnerId, CollarProvider.Own, null);
        typeof(Collar).GetProperty("Id")!.SetValue(collar, CollarId);
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);

        var result = await _sut.Handle(new DeactivateCollarLostModeCommand(CollarId, OwnerId), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainMatch("*no está activo*");
    }

    [Fact]
    public async Task Handle_WrongOwner_ReturnsAccessDenied()
    {
        var collar = Collar.Register(Guid.NewGuid(), Guid.NewGuid(), CollarProvider.Own, null);
        typeof(Collar).GetProperty("Id")!.SetValue(collar, CollarId);
        collar.ActivateLostMode(Guid.NewGuid());
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);

        var result = await _sut.Handle(new DeactivateCollarLostModeCommand(CollarId, OwnerId), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Access denied.");
    }
}

using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Collars.Commands.UpdateCollarNotificationPreferences;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Collars;

namespace PawTrack.UnitTests.Collars.Handlers;

public sealed class UpdateCollarNotificationPreferencesCommandHandlerTests
{
    private readonly ICollarRepository _collarRepo = Substitute.For<ICollarRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private readonly UpdateCollarNotificationPreferencesCommandHandler _sut;
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid CollarId = Guid.NewGuid();

    public UpdateCollarNotificationPreferencesCommandHandlerTests()
    {
        _sut = new UpdateCollarNotificationPreferencesCommandHandler(_collarRepo, _uow);
    }

    private Collar MakeCollar(Guid ownerId, bool active = true)
    {
        var collar = Collar.Register(Guid.NewGuid(), ownerId, CollarProvider.Own, null);
        typeof(Collar).GetProperty("Id")!.SetValue(collar, CollarId);
        if (!active) collar.Deactivate();
        return collar;
    }

    [Fact]
    public async Task Handle_HappyPath_UpdatesPreferencesAndSaves()
    {
        var collar = MakeCollar(OwnerId);
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);

        var result = await _sut.Handle(
            new UpdateCollarNotificationPreferencesCommand(CollarId, OwnerId, false, 90, true, 25),
            default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OfflineThresholdMinutes.Should().Be(90);
        result.Value.BatteryAlertThresholdPercent.Should().Be(25);
        _collarRepo.Received(1).Update(collar);
        await _uow.Received(1).SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_CollarNotFound_ReturnsFailure()
    {
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns((Collar?)null);

        var result = await _sut.Handle(
            new UpdateCollarNotificationPreferencesCommand(CollarId, OwnerId, true, 120, true, 20),
            default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainMatch("*no encontrado*");
    }

    [Fact]
    public async Task Handle_WrongOwner_ReturnsAccessDenied()
    {
        var collar = MakeCollar(Guid.NewGuid());
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);

        var result = await _sut.Handle(
            new UpdateCollarNotificationPreferencesCommand(CollarId, OwnerId, true, 120, true, 20),
            default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Access denied.");
    }

    [Fact]
    public async Task Handle_InvalidThreshold_ReturnsFailureAndDoesNotSave()
    {
        var collar = MakeCollar(OwnerId);
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);

        var result = await _sut.Handle(
            new UpdateCollarNotificationPreferencesCommand(CollarId, OwnerId, true, 5, true, 20),
            default);

        result.IsFailure.Should().BeTrue();
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

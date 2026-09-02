using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Collars.Queries.GetCollarLostModeStatus;
using PawTrack.Domain.Collars;

namespace PawTrack.UnitTests.Collars.Handlers;

public sealed class GetCollarLostModeStatusQueryHandlerTests
{
    private readonly ICollarRepository _collarRepo = Substitute.For<ICollarRepository>();
    private readonly GetCollarLostModeStatusQueryHandler _sut;
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid CollarId = Guid.NewGuid();

    public GetCollarLostModeStatusQueryHandlerTests()
    {
        _sut = new GetCollarLostModeStatusQueryHandler(_collarRepo);
    }

    [Fact]
    public async Task Handle_LostCollar_ReturnsStatusWithEventId()
    {
        var collar = Collar.Register(Guid.NewGuid(), OwnerId, CollarProvider.Own, null);
        typeof(Collar).GetProperty("Id")!.SetValue(collar, CollarId);
        var lostPetEventId = Guid.NewGuid();
        collar.ActivateLostMode(lostPetEventId);
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);

        var result = await _sut.Handle(new GetCollarLostModeStatusQuery(CollarId, OwnerId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsLost.Should().BeTrue();
        result.Value.LostPetEventId.Should().Be(lostPetEventId);
    }

    [Fact]
    public async Task Handle_WrongOwner_ReturnsAccessDenied()
    {
        var collar = Collar.Register(Guid.NewGuid(), Guid.NewGuid(), CollarProvider.Own, null);
        typeof(Collar).GetProperty("Id")!.SetValue(collar, CollarId);
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);

        var result = await _sut.Handle(new GetCollarLostModeStatusQuery(CollarId, OwnerId), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Access denied.");
    }
}

using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Collars.Queries.GetCollarLocationHistoryRange;
using PawTrack.Domain.Collars;

namespace PawTrack.UnitTests.Collars.Handlers;

public sealed class GetCollarLocationHistoryRangeQueryHandlerTests
{
    private readonly ICollarRepository _collarRepo = Substitute.For<ICollarRepository>();
    private readonly GetCollarLocationHistoryRangeQueryHandler _sut;
    private static readonly Guid OwnerId = Guid.NewGuid();
    private static readonly Guid CollarId = Guid.NewGuid();

    public GetCollarLocationHistoryRangeQueryHandlerTests()
    {
        _sut = new GetCollarLocationHistoryRangeQueryHandler(_collarRepo);
    }

    private static Collar MakeCollar(Guid ownerId)
    {
        var collar = Collar.Register(Guid.NewGuid(), ownerId, CollarProvider.Own, null);
        typeof(Collar).GetProperty("Id")!.SetValue(collar, CollarId);
        return collar;
    }

    [Fact]
    public async Task Handle_Owner_ReturnsPoints()
    {
        var collar = MakeCollar(OwnerId);
        var point = CollarLocation.Record(CollarId, 9.9, -84.1, 5);
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);
        _collarRepo.GetLocationHistoryRangeAsync(
                CollarId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { point });

        var result = await _sut.Handle(
            new GetCollarLocationHistoryRangeQuery(
                CollarId, OwnerId, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow),
            default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(p => p.Lat == 9.9 && p.Lng == -84.1);
    }

    [Fact]
    public async Task Handle_WrongOwner_ReturnsAccessDenied()
    {
        var collar = MakeCollar(Guid.NewGuid());
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);

        var result = await _sut.Handle(
            new GetCollarLocationHistoryRangeQuery(
                CollarId, OwnerId, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow),
            default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Access denied.");
    }

    [Fact]
    public async Task Handle_CollarNotFound_ReturnsFailure()
    {
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns((Collar?)null);

        var result = await _sut.Handle(
            new GetCollarLocationHistoryRangeQuery(
                CollarId, OwnerId, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow),
            default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainMatch("*no encontrado*");
    }

    [Fact]
    public async Task Handle_FromBeyondRetention_IsClampedToThirtyDays()
    {
        var collar = MakeCollar(OwnerId);
        _collarRepo.GetByIdAsync(CollarId, Arg.Any<CancellationToken>()).Returns(collar);
        _collarRepo.GetLocationHistoryRangeAsync(
                CollarId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CollarLocation>());

        await _sut.Handle(
            new GetCollarLocationHistoryRangeQuery(
                CollarId, OwnerId, DateTimeOffset.UtcNow.AddDays(-90), DateTimeOffset.UtcNow),
            default);

        await _collarRepo.Received(1).GetLocationHistoryRangeAsync(
            CollarId,
            Arg.Is<DateTimeOffset>(d => d > DateTimeOffset.UtcNow.AddDays(-31)),
            Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}

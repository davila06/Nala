using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Sightings.DTOs;
using PawTrack.Application.Sightings.Queries.GetActiveFoundPets;
using PawTrack.Domain.Common;

namespace PawTrack.UnitTests.Sightings.Queries;

/// <summary>
/// Round-6 security: unbounded maxResults would allow a DoS via a single API call.
/// The handler must clamp the value to [1, 100].
/// </summary>
public sealed class GetActiveFoundPetsQueryHandlerTests
{
    private readonly IFoundPetRepository _repo = Substitute.For<IFoundPetRepository>();
    private readonly GetActiveFoundPetsQueryHandler _sut;

    public GetActiveFoundPetsQueryHandlerTests()
    {
        _sut = new GetActiveFoundPetsQueryHandler(_repo);
        _repo.GetOpenReportsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
             .Returns(new List<Domain.Sightings.FoundPetReport>());
    }

    [Fact]
    public async Task Handle_MaxResultsOver100_ClampsTo100()
    {
        await _sut.Handle(new GetActiveFoundPetsQuery(999), CancellationToken.None);

        await _repo.Received(1).GetOpenReportsAsync(100, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MaxResultsZeroOrNegative_ClampsTo1()
    {
        await _sut.Handle(new GetActiveFoundPetsQuery(0), CancellationToken.None);

        await _repo.Received(1).GetOpenReportsAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MaxResultsWithinBounds_PassesThrough()
    {
        await _sut.Handle(new GetActiveFoundPetsQuery(25), CancellationToken.None);

        await _repo.Received(1).GetOpenReportsAsync(25, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DefaultMaxResults_ReturnsWithin100()
    {
        await _sut.Handle(new GetActiveFoundPetsQuery(), CancellationToken.None);

        await _repo.Received(1).GetOpenReportsAsync(
            Arg.Is<int>(n => n >= 1 && n <= 100),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PublicProjection_RoundsFinderCoordinates()
    {
        var report = Domain.Sightings.FoundPetReport.Create(
            Domain.Pets.PetSpecies.Dog,
            null,
            "Brown",
            "Medium",
            9.934739,
            -84.087502,
            "Finder",
            "+50688881234",
            "Near the park");
        _repo.GetOpenReportsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([report]);

        var result = await _sut.Handle(new GetActiveFoundPetsQuery(), CancellationToken.None);

        result.Value.Should().ContainSingle();
        result.Value[0].FoundLat.Should().Be(9.935);
        result.Value[0].FoundLng.Should().Be(-84.088);
    }
}

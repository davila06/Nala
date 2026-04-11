using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Sightings.Queries.GetPublicMapEvents;
using PawTrack.Domain.LostPets;
using PawTrack.Domain.Sightings;

namespace PawTrack.UnitTests.Sightings.Queries;

public sealed class GetPublicMapEventsQueryHandlerTests
{
    private readonly ISightingRepository _sightingRepo = Substitute.For<ISightingRepository>();
    private readonly ILostPetRepository _lostPetRepo = Substitute.For<ILostPetRepository>();
    private readonly GetPublicMapEventsQueryHandler _sut;

    public GetPublicMapEventsQueryHandlerTests()
    {
        _sut = new GetPublicMapEventsQueryHandler(_sightingRepo, _lostPetRepo);
    }

    [Fact]
    public async Task Handle_ReturnsCombinedResults()
    {
        // Arrange — one sighting and one active lost pet both within BBOX
        var petId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var sighting = Sighting.Create(petId, null, 9.9281, -84.0907, "Gray cat", DateTimeOffset.UtcNow);
        var lostPet = LostPetEvent.Create(petId, ownerId, "Lost dog", 9.93, -84.09, DateTimeOffset.UtcNow);

        var sightings = new List<Sighting> { sighting }.AsReadOnly() as IReadOnlyList<Sighting>;
        var lostPets = new List<LostPetEvent> { lostPet }.AsReadOnly() as IReadOnlyList<LostPetEvent>;

        _sightingRepo.GetInBBoxAsync(10, 9.9, -84, -85, Arg.Any<CancellationToken>())
            .Returns(sightings!);
        _lostPetRepo.GetActiveLostPetsInBBoxAsync(10, 9.9, -84, -85, Arg.Any<CancellationToken>())
            .Returns(lostPets!);

        var query = new GetPublicMapEventsQuery(10, 9.9, -84, -85);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(2);
        result.Value.Should().Contain(e => e.EventType == "Sighting");
        result.Value.Should().Contain(e => e.EventType == "LostPet");
    }

    [Fact]
    public async Task Handle_EmptyBBox_ReturnsEmptyList()
    {
        var empty = Array.Empty<Sighting>() as IReadOnlyList<Sighting>;
        var emptyLpe = Array.Empty<LostPetEvent>() as IReadOnlyList<LostPetEvent>;

        _sightingRepo.GetInBBoxAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<CancellationToken>())
            .Returns(empty!);
        _lostPetRepo.GetActiveLostPetsInBBoxAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<CancellationToken>())
            .Returns(emptyLpe!);

        var result = await _sut.Handle(new GetPublicMapEventsQuery(10, 9, -84, -85), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_LostPetWithoutCoordinates_ExcludedFromMap()
    {
        var petId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        // LostPetEvent with null coordinates
        var lostPetNoCoords = LostPetEvent.Create(petId, ownerId, "Lost dog", null, null, DateTimeOffset.UtcNow);

        var empty = Array.Empty<Sighting>() as IReadOnlyList<Sighting>;
        var lostPets = new List<LostPetEvent> { lostPetNoCoords }.AsReadOnly() as IReadOnlyList<LostPetEvent>;

        _sightingRepo.GetInBBoxAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<CancellationToken>())
            .Returns(empty!);
        _lostPetRepo.GetActiveLostPetsInBBoxAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<CancellationToken>())
            .Returns(lostPets!);

        var result = await _sut.Handle(new GetPublicMapEventsQuery(10, 9.9, -84, -85), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().BeEmpty("lost pet without coordinates must be excluded from the map");
    }
}

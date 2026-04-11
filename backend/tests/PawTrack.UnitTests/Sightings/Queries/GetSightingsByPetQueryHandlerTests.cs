using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Sightings.Queries.GetSightingsByPet;
using PawTrack.Application.Sightings.Scoring;
using PawTrack.Domain.LostPets;
using PawTrack.Domain.Pets;
using PawTrack.Domain.Sightings;

namespace PawTrack.UnitTests.Sightings.Queries;

public sealed class GetSightingsByPetQueryHandlerTests
{
    private readonly ISightingRepository _sightingRepository = Substitute.For<ISightingRepository>();
    private readonly IPetRepository _petRepository = Substitute.For<IPetRepository>();
    private readonly ILostPetRepository _lostPetRepository = Substitute.For<ILostPetRepository>();
    private readonly ISightingPriorityScorer _priorityScorer = new SightingPriorityScorer();

    [Fact]
    public async Task Handle_OrdersSightingsByPriorityAndProjectsPriorityFields()
    {
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Luna", PetSpecies.Dog, "Labrador", null);
        var lostReport = LostPetEvent.Create(
            pet.Id,
            ownerId,
            "Lost near the park",
            9.9381,
            -84.1002,
            DateTimeOffset.UtcNow.AddMinutes(-50));

        var lowPrioritySighting = Sighting.Create(
            pet.Id,
            lostReport.Id,
            10.1000,
            -84.4000,
            null,
            DateTimeOffset.UtcNow.AddHours(-30));

        var urgentSighting = Sighting.Create(
            pet.Id,
            lostReport.Id,
            9.9382,
            -84.1001,
            "Looks injured and moving east",
            DateTimeOffset.UtcNow.AddMinutes(-10));
        urgentSighting.SetPhoto("https://cdn.example.com/sighting.jpg");

        _petRepository.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        _lostPetRepository.GetActiveByPetIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(lostReport);
        _sightingRepository.GetByPetIdAsync(pet.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Sighting> { lowPrioritySighting, urgentSighting }.AsReadOnly());

        var sut = new GetSightingsByPetQueryHandler(
            _sightingRepository,
            _petRepository,
            _lostPetRepository,
            _priorityScorer);

        var result = await sut.Handle(new GetSightingsByPetQuery(pet.Id, ownerId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Id.Should().Be(urgentSighting.Id.ToString());
        result.Value[0].PriorityBadge.Should().Be(nameof(SightingPriorityBadge.Urgent));
        result.Value[0].PriorityScore.Should().BeGreaterThan(result.Value[1].PriorityScore);
        result.Value[0].RecommendedAction.Should().NotBeNullOrWhiteSpace();
    }
}
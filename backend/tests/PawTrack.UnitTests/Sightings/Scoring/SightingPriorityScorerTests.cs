using FluentAssertions;
using PawTrack.Application.Sightings.Scoring;
using PawTrack.Domain.LostPets;
using PawTrack.Domain.Pets;
using PawTrack.Domain.Sightings;

namespace PawTrack.UnitTests.Sightings.Scoring;

public sealed class SightingPriorityScorerTests
{
    private static readonly DateTimeOffset Baseline = new(2026, 4, 4, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Score_WhenSightingIsNearRecentAndHasPhoto_ReturnsUrgent()
    {
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Luna", PetSpecies.Dog, "Labrador", null);
        var lostReport = LostPetEvent.Create(
            pet.Id,
            ownerId,
            "Lost near Sabana",
            9.9381,
            -84.1002,
            Baseline.AddMinutes(-40));

        var sighting = Sighting.Create(
            pet.Id,
            lostReport.Id,
            9.9383,
            -84.1001,
            "Seen crossing the park",
            Baseline.AddMinutes(-20));

        sighting.SetPhoto("https://cdn.example.com/sightings/luna.jpg");

        var sut = new SightingPriorityScorer();

        var result = sut.Score(pet, lostReport, sighting, Baseline);

        result.Score.Should().BeGreaterThanOrEqualTo(80);
        result.Badge.Should().Be(SightingPriorityBadge.Urgent);
        result.RecommendedAction.Should().Contain("Contacta", Exactly.Once());
    }

    [Fact]
    public void Score_WhenSightingIsOldFarAndWithoutPhoto_ReturnsObserve()
    {
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Michi", PetSpecies.Cat, null, null);
        var lostReport = LostPetEvent.Create(
            pet.Id,
            ownerId,
            "Missing since yesterday",
            9.9350,
            -84.0910,
            Baseline.AddHours(-28));

        var sighting = Sighting.Create(
            pet.Id,
            lostReport.Id,
            10.0025,
            -84.2200,
            null,
            Baseline.AddHours(-26));

        var sut = new SightingPriorityScorer();

        var result = sut.Score(pet, lostReport, sighting, Baseline);

        result.Score.Should().BeLessThan(50);
        result.Badge.Should().Be(SightingPriorityBadge.Observe);
        result.RecommendedAction.Should().Contain("Mantén", Exactly.Once());
    }

    [Fact]
    public void Score_WhenLostReportHasNoCoordinates_FallsBackToValidate()
    {
        var ownerId = Guid.NewGuid();
        var pet = Pet.Create(ownerId, "Nube", PetSpecies.Dog, "Mixed", null);
        var lostReport = LostPetEvent.Create(
            pet.Id,
            ownerId,
            "No exact location available",
            null,
            null,
            Baseline.AddHours(-6));

        var sighting = Sighting.Create(
            pet.Id,
            lostReport.Id,
            9.9400,
            -84.0800,
            "Looks similar",
            Baseline.AddHours(-2));

        var sut = new SightingPriorityScorer();

        var result = sut.Score(pet, lostReport, sighting, Baseline);

        result.Badge.Should().Be(SightingPriorityBadge.Validate);
        result.Score.Should().BeInRange(50, 79);
    }
}
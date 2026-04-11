using FluentAssertions;
using PawTrack.Application.LostPets.SearchRadius;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.LostPets.SearchRadius;

public sealed class LostPetSearchRadiusCalculatorTests
{
    private static readonly DateTimeOffset ReferenceTime = new(2026, 4, 4, 18, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(PetSpecies.Dog, "Labrador", -1.5, 500)]
    [InlineData(PetSpecies.Dog, "Labrador", -3, 750)]
    [InlineData(PetSpecies.Dog, "Labrador", -10, 1000)]
    [InlineData(PetSpecies.Dog, "Labrador", -30, 1500)]
    [InlineData(PetSpecies.Cat, null, -4, 375)]
    [InlineData(PetSpecies.Rabbit, null, -30, 100)]
    public void Calculate_ReturnsExpectedDynamicRing(
        PetSpecies species,
        string? breed,
        double hoursOffset,
        int expectedRadius)
    {
        var sut = new LostPetSearchRadiusCalculator();

        var radius = sut.Calculate(species, breed, ReferenceTime.AddHours(hoursOffset), ReferenceTime);

        radius.Should().Be(expectedRadius);
    }
}
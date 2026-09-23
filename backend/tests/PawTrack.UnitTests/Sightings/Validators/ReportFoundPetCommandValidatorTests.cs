using FluentAssertions;
using PawTrack.Application.Sightings.Commands.ReportFoundPet;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Sightings.Validators;

public sealed class ReportFoundPetCommandValidatorTests
{
    [Fact]
    public async Task RejectsReportWithoutPrivacyConsent()
    {
        var command = new ReportFoundPetCommand(
            PetSpecies.Dog,
            null,
            "Negro",
            "Mediano",
            9.93,
            -84.08,
            "Finder",
            "8888-8888",
            null,
            null,
            null,
            false);

        var result = await new ReportFoundPetCommandValidator().ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "PrivacyConsent");
    }
}

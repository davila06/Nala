using FluentAssertions;
using FluentValidation.TestHelper;
using PawTrack.Application.Pets.Commands.ReactivatePet;

namespace PawTrack.UnitTests.Pets.Validators;

public sealed class ReactivatePetCommandValidatorTests
{
    private readonly ReactivatePetCommandValidator _sut = new();

    [Fact]
    public void Validate_ValidCommand_PassesAllRules()
    {
        var result = _sut.TestValidate(new ReactivatePetCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyPetId_FailsWithMessage()
    {
        var result = _sut.TestValidate(new ReactivatePetCommand(Guid.Empty, Guid.NewGuid()));

        result.ShouldHaveValidationErrorFor(x => x.PetId)
              .WithErrorMessage("Pet ID must not be empty.");
    }

    [Fact]
    public void Validate_EmptyRequestingUserId_FailsWithMessage()
    {
        var result = _sut.TestValidate(new ReactivatePetCommand(Guid.NewGuid(), Guid.Empty));

        result.ShouldHaveValidationErrorFor(x => x.RequestingUserId)
              .WithErrorMessage("Requesting user ID must not be empty.");
    }
}

using FluentAssertions;
using FluentValidation.TestHelper;
using PawTrack.Application.Fosters.Commands.CloseCustody;

namespace PawTrack.UnitTests.Fosters.Validators;

/// <summary>
/// Round-7 security: close-custody Outcome field must be bounded.
/// </summary>
public sealed class CloseCustodyCommandValidatorTests
{
    private readonly CloseCustodyCommandValidator _sut = new();

    private static CloseCustodyCommand Valid(string outcome = "Reunited") =>
        new(Guid.NewGuid(), Guid.NewGuid(), outcome);

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _sut.TestValidate(Valid());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_OutcomeAtMaxLength_Passes()
    {
        var result = _sut.TestValidate(Valid(new string('x', 200)));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_OutcomeExceedsMaxLength_Fails()
    {
        var result = _sut.TestValidate(Valid(new string('x', 201)));
        result.ShouldHaveValidationErrorFor(x => x.Outcome);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespaceOutcome_Fails(string outcome)
    {
        var result = _sut.TestValidate(Valid(outcome));
        result.ShouldHaveValidationErrorFor(x => x.Outcome);
    }
}

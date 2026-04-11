using FluentAssertions;
using FluentValidation.TestHelper;
using PawTrack.Application.Fosters.Commands.StartCustody;

namespace PawTrack.UnitTests.Fosters.Validators;

/// <summary>
/// Round-7 security: start-custody inputs must be bounded to prevent
/// oversized Note persisting to the database and invalid ExpectedDays arithmetic.
/// </summary>
public sealed class StartCustodyCommandValidatorTests
{
    private readonly StartCustodyCommandValidator _sut = new();

    private static StartCustodyCommand Valid(int days = 7, string? note = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), days, note);

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _sut.TestValidate(Valid());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_NullNote_Passes()
    {
        var result = _sut.TestValidate(Valid(note: null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_NoteAtMaxLength_Passes()
    {
        var result = _sut.TestValidate(Valid(note: new string('x', 500)));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_NoteExceedsMaxLength_Fails()
    {
        var result = _sut.TestValidate(Valid(note: new string('x', 501)));
        result.ShouldHaveValidationErrorFor(x => x.Note);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(366)]
    [InlineData(9999)]
    public void Validate_InvalidExpectedDays_Fails(int days)
    {
        var result = _sut.TestValidate(Valid(days: days));
        result.ShouldHaveValidationErrorFor(x => x.ExpectedDays);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(365)]
    public void Validate_ValidExpectedDays_Passes(int days)
    {
        var result = _sut.TestValidate(Valid(days: days));
        result.ShouldNotHaveAnyValidationErrors();
    }
}

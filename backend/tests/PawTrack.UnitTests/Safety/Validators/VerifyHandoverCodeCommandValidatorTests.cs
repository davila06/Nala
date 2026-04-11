using FluentValidation.TestHelper;
using PawTrack.Application.Safety.Commands.VerifyHandoverCode;

namespace PawTrack.UnitTests.Safety.Validators;

public sealed class VerifyHandoverCodeCommandValidatorTests
{
    private readonly VerifyHandoverCodeCommandValidator _sut = new();

    private static VerifyHandoverCodeCommand ValidCommand(string code = "4827") =>
        new(Guid.NewGuid(), Guid.NewGuid(), code);

    [Fact]
    public void ValidCode_ShouldPassValidation()
    {
        _sut.TestValidate(ValidCommand("1234")).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    public void Code_Empty_ShouldFail(string code)
    {
        _sut.TestValidate(ValidCommand(code)).ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Theory]
    [InlineData("123")]        // 3 digits — too short
    [InlineData("12345")]      // 5 digits — too long
    [InlineData("abcd")]       // letters
    [InlineData("12 4")]       // space
    [InlineData("12.4")]       // dot
    public void Code_NotFourDigits_ShouldFail(string code)
    {
        _sut.TestValidate(ValidCommand(code)).ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Code_VeryLongString_ShouldFail()
    {
        var tooLong = new string('1', 4096);
        _sut.TestValidate(ValidCommand(tooLong)).ShouldHaveValidationErrorFor(x => x.Code);
    }
}

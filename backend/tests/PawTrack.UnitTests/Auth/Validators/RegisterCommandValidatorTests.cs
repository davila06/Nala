using FluentValidation.TestHelper;
using PawTrack.Application.Auth.Commands.Register;

namespace PawTrack.UnitTests.Auth.Validators;

public sealed class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _sut = new();

    [Fact]
    public void Name_Empty_ShouldFail()
    {
        var cmd = new RegisterCommand(string.Empty, "test@example.com", "Password1");
        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Name_TooLong_ShouldFail()
    {
        var cmd = new RegisterCommand(new string('a', 101), "test@example.com", "Password1");
        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@nodomain")]
    [InlineData("")]
    public void Email_Invalid_ShouldFail(string email)
    {
        var cmd = new RegisterCommand("Test User", email, "Password1");
        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("short1")]         // < 8 chars
    [InlineData("nouppercase1!")]  // no uppercase
    [InlineData("NOLOWER1!")]      // no lowercase
    [InlineData("NoDigitHere!")]   // no digit
    [InlineData("NoSpecialChar1")] // no special character
    public void Password_Invalid_ShouldFail(string password)
    {
        var cmd = new RegisterCommand("Test User", "test@example.com", password);
        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void ValidCommand_ShouldPassValidation()
    {
        var cmd = new RegisterCommand("Denis Avila", "denis@pawtrack.cr", "SecurePass1!");
        _sut.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

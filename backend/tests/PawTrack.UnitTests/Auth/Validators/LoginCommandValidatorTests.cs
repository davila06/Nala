using FluentValidation.TestHelper;
using PawTrack.Application.Auth.Commands.Login;

namespace PawTrack.UnitTests.Auth.Validators;

public sealed class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _sut = new();

    [Theory]
    [InlineData("")]
    [InlineData("notanemail")]
    public void Email_Invalid_ShouldFail(string email)
    {
        var cmd = new LoginCommand(email, "password");
        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Password_Empty_ShouldFail()
    {
        var cmd = new LoginCommand("user@example.com", string.Empty);
        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var cmd = new LoginCommand("user@example.com", "anypassword");
        _sut.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

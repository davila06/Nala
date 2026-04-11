using FluentValidation.TestHelper;
using PawTrack.Application.Auth.Commands.ResetPassword;

namespace PawTrack.UnitTests.Auth.Validators;

public sealed class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _sut = new();

    [Fact]
    public void Token_Empty_ShouldFail()
    {
        var cmd = new ResetPasswordCommand(string.Empty, "SecurePass1!");
        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Token);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("alllowercase123!")]
    [InlineData("ALLUPPERCASE123!")]
    [InlineData("NoNumber!")]
    public void NewPassword_Weak_ShouldFail(string password)
    {
        var cmd = new ResetPasswordCommand("Abcdefghijklmnopqrstuvwxyz0123456789-_", password);
        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void ValidRequest_ShouldPass()
    {
        var cmd = new ResetPasswordCommand("Abcdefghijklmnopqrstuvwxyz0123456789-_", "SecurePass1!");
        _sut.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

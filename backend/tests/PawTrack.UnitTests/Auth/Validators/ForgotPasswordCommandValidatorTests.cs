using FluentValidation.TestHelper;
using PawTrack.Application.Auth.Commands.ForgotPassword;

namespace PawTrack.UnitTests.Auth.Validators;

public sealed class ForgotPasswordCommandValidatorTests
{
    private readonly ForgotPasswordCommandValidator _sut = new();

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Email_Invalid_ShouldFail(string email)
    {
        var cmd = new ForgotPasswordCommand(email);
        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Email_Valid_ShouldPass()
    {
        var cmd = new ForgotPasswordCommand("owner@example.com");
        _sut.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }
}

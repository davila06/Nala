using FluentValidation.TestHelper;
using PawTrack.Application.Clinics.Commands.RegisterClinic;

namespace PawTrack.UnitTests.Clinics.Validators;

public sealed class RegisterClinicCommandValidatorTests
{
    private readonly RegisterClinicCommandValidator _sut = new();

    private static RegisterClinicCommand ValidCommand() => new(
        Name: "Clínica Paws CR",
        LicenseNumber: "SENASA-12345",
        Address: "San José, Costa Rica",
        Lat: 9.93m,
        Lng: -84.08m,
        ContactEmail: "clinica@pawtrack.cr",
        Password: "SecurePass1!");

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        _sut.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    public void Name_Empty_ShouldFail(string name)
    {
        _sut.TestValidate(ValidCommand() with { Name = name })
            .ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Name_TooLong_ShouldFail()
    {
        _sut.TestValidate(ValidCommand() with { Name = new string('A', 201) })
            .ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData("")]
    public void LicenseNumber_Empty_ShouldFail(string license)
    {
        _sut.TestValidate(ValidCommand() with { LicenseNumber = license })
            .ShouldHaveValidationErrorFor(x => x.LicenseNumber);
    }

    [Fact]
    public void LicenseNumber_TooLong_ShouldFail()
    {
        _sut.TestValidate(ValidCommand() with { LicenseNumber = new string('X', 101) })
            .ShouldHaveValidationErrorFor(x => x.LicenseNumber);
    }

    [Theory]
    [InlineData("")]
    public void Address_Empty_ShouldFail(string address)
    {
        _sut.TestValidate(ValidCommand() with { Address = address })
            .ShouldHaveValidationErrorFor(x => x.Address);
    }

    [Fact]
    public void Address_TooLong_ShouldFail()
    {
        _sut.TestValidate(ValidCommand() with { Address = new string('A', 301) })
            .ShouldHaveValidationErrorFor(x => x.Address);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@nodomain.com")]
    [InlineData("")]
    public void Email_Invalid_ShouldFail(string email)
    {
        _sut.TestValidate(ValidCommand() with { ContactEmail = email })
            .ShouldHaveValidationErrorFor(x => x.ContactEmail);
    }

    [Theory]
    [InlineData("short1")]         // < 8 chars
    [InlineData("nouppercase1!")]  // no uppercase
    [InlineData("NOLOWER1!")]      // no lowercase
    [InlineData("NoDigitHere!")]   // no digit
    [InlineData("NoSpecialChar1")] // no special char
    public void Password_WeakPassword_ShouldFail(string password)
    {
        _sut.TestValidate(ValidCommand() with { Password = password })
            .ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Lat_OutOfRange_ShouldFail()
    {
        _sut.TestValidate(ValidCommand() with { Lat = 91m })
            .ShouldHaveValidationErrorFor(x => x.Lat);
    }

    [Fact]
    public void Lng_OutOfRange_ShouldFail()
    {
        _sut.TestValidate(ValidCommand() with { Lng = -181m })
            .ShouldHaveValidationErrorFor(x => x.Lng);
    }
}

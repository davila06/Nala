using FluentAssertions;
using FluentValidation.TestHelper;
using PawTrack.Application.Sightings.Commands.ReportFoundPet;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Sightings.Validators;

/// <summary>
/// Round-9 security: ReportFoundPetCommandValidator.ContactPhone must enforce the same
/// format regex as ReportLostPetCommandValidator to prevent injection of non-phone
/// characters into the contact phone field.
/// </summary>
public sealed class ReportFoundPetContactPhoneValidatorTests
{
    private readonly ReportFoundPetCommandValidator _sut = new();

    private static ReportFoundPetCommand Valid(string phone = "+506 8888-1234") =>
        new(
            PetSpecies.Dog,
            null, null, null,
            9.9281, -84.0907,
            "Juan Pérez",
            phone,
            null, null, null);

    // ── Valid phone formats ───────────────────────────────────────────────────

    [Theory]
    [InlineData("+506 8888-1234")]
    [InlineData("8888-1234")]
    [InlineData("(506) 8888 1234")]
    [InlineData("8888.1234")]
    public void Validate_ValidPhoneNumber_Passes(string phone)
    {
        var result = _sut.TestValidate(Valid(phone));
        result.ShouldNotHaveValidationErrorFor(x => x.ContactPhone);
    }

    // ── Invalid phone formats ─────────────────────────────────────────────────

    [Theory]
    [InlineData("abc-xyz")]              // letters
    [InlineData("hack'; DROP TABLE--")] // SQL-like input
    [InlineData("<script>alert(1)</script>")] // HTML/XSS
    [InlineData("12345")]               // too short (< 7 chars of digits)
    public void Validate_InvalidPhoneCharacters_FailsValidation(string phone)
    {
        var result = _sut.TestValidate(Valid(phone));
        result.ShouldHaveValidationErrorFor(x => x.ContactPhone);
    }

    // ── Length bounds ─────────────────────────────────────────────────────────

    [Fact]
    public void Validate_PhoneTooLong_FailsValidation()
    {
        var longPhone = new string('1', 31); // exceeds 30 char max
        var result = _sut.TestValidate(Valid(longPhone));
        result.ShouldHaveValidationErrorFor(x => x.ContactPhone);
    }

    [Fact]
    public void Validate_EmptyPhone_FailsValidation()
    {
        var result = _sut.TestValidate(Valid(string.Empty));
        result.ShouldHaveValidationErrorFor(x => x.ContactPhone);
    }
}

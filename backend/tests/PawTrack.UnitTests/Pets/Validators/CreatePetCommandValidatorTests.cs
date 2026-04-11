using FluentAssertions;
using FluentValidation;
using FluentValidation.TestHelper;
using PawTrack.Application.Pets.Commands.CreatePet;
using PawTrack.Domain.Pets;

namespace PawTrack.UnitTests.Pets.Validators;

public sealed class CreatePetCommandValidatorTests
{
    private readonly CreatePetCommandValidator _validator = new();

    private static CreatePetCommand ValidCommand(
        string name = "Firulais",
        PetSpecies species = PetSpecies.Dog,
        byte[]? photo = null,
        string? contentType = null) =>
        new(Guid.NewGuid(), name, species, null, null, photo, contentType, null);

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyName_FailsValidation(string name)
    {
        var result = _validator.TestValidate(ValidCommand(name: name));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NameTooLong_FailsValidation()
    {
        var result = _validator.TestValidate(ValidCommand(name: new string('A', 101)));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_PhotoTooLarge_FailsValidation()
    {
        var bigPhoto = new byte[6 * 1024 * 1024]; // 6 MB
        var result = _validator.TestValidate(ValidCommand(photo: bigPhoto, contentType: "image/jpeg"));
        result.ShouldHaveValidationErrorFor(x => x.PhotoBytes);
    }

    [Fact]
    public void Validate_InvalidMimeType_FailsValidation()
    {
        var photo = new byte[100];
        var result = _validator.TestValidate(ValidCommand(photo: photo, contentType: "application/pdf"));
        result.ShouldHaveValidationErrorFor(x => x.PhotoContentType);
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    public void Validate_AllowedMimeTypes_PassesValidation(string contentType)
    {
        var photo = new byte[100];
        var result = _validator.TestValidate(ValidCommand(photo: photo, contentType: contentType));
        result.ShouldNotHaveValidationErrorFor(x => x.PhotoContentType);
    }

    [Fact]
    public void Validate_PhotoWithInvalidMagicBytes_FailsValidation()
    {
        // %PDF header disguised as image/jpeg — classic web-shell disguise
        byte[] malicious = [0x25, 0x50, 0x44, 0x46, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        var result = _validator.TestValidate(ValidCommand(photo: malicious, contentType: "image/jpeg"));
        result.ShouldHaveValidationErrorFor(x => x.PhotoBytes);
    }

    [Fact]
    public void Validate_PhotoWithJpegMagicBytes_PassesValidation()
    {
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
        var result = _validator.TestValidate(ValidCommand(photo: jpeg, contentType: "image/jpeg"));
        result.ShouldNotHaveValidationErrorFor(x => x.PhotoBytes);
    }

    [Fact]
    public void Validate_PhotoWithPngMagicBytes_PassesValidation()
    {
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];
        var result = _validator.TestValidate(ValidCommand(photo: png, contentType: "image/png"));
        result.ShouldNotHaveValidationErrorFor(x => x.PhotoBytes);
    }

    [Fact]
    public void Validate_FutureBirthDate_FailsValidation()
    {
        var cmd = new CreatePetCommand(
            Guid.NewGuid(), "Max", PetSpecies.Dog, null,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            null, null, null);

        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.BirthDate);
    }
}

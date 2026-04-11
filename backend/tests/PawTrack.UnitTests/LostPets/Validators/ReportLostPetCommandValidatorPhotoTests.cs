using FluentValidation.TestHelper;
using PawTrack.Application.LostPets.Commands.ReportLostPet;

namespace PawTrack.UnitTests.LostPets.Validators;

public sealed class ReportLostPetCommandValidatorPhotoTests
{
    private readonly ReportLostPetCommandValidator _sut = new();

    private static ReportLostPetCommand BaseCommand(
        byte[]? photo = null,
        string? contentType = null) =>
        new(
            PetId:              Guid.NewGuid(),
            RequestingUserId:   Guid.NewGuid(),
            Description:        null,
            PublicMessage:      null,
            LastSeenLat:        9.93,
            LastSeenLng:        -84.08,
            LastSeenAt:         DateTimeOffset.UtcNow.AddMinutes(-10),
            PhotoBytes:         photo,
            PhotoContentType:   contentType,
            PhotoFileName:      photo is null ? null : "photo.jpg",
            ContactName:        null,
            ContactPhone:       null,
            RewardAmount:       null,
            RewardNote:         null);

    [Fact]
    public void NoPhoto_ShouldPassValidation()
    {
        _sut.TestValidate(BaseCommand()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Photo_ValidJpegMagicBytesAndMime_ShouldPass()
    {
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
        _sut.TestValidate(BaseCommand(jpeg, "image/jpeg")).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Photo_TooLarge_ShouldFail()
    {
        byte[] bigJpeg = new byte[6 * 1024 * 1024]; // 6 MB — starts with zeros, not JPEG
        bigJpeg[0] = 0xFF; bigJpeg[1] = 0xD8;      // fix header so only size fails
        _sut.TestValidate(BaseCommand(bigJpeg, "image/jpeg"))
            .ShouldHaveValidationErrorFor(x => x.PhotoBytes);
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("text/html")]
    [InlineData("application/octet-stream")]
    public void Photo_DisallowedMime_ShouldFail(string mime)
    {
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
        _sut.TestValidate(BaseCommand(jpeg, mime))
            .ShouldHaveValidationErrorFor(x => x.PhotoContentType);
    }

    [Fact]
    public void Photo_ValidMimeButPdfMagicBytes_ShouldFail()
    {
        byte[] pdf = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        _sut.TestValidate(BaseCommand(pdf, "image/jpeg"))
            .ShouldHaveValidationErrorFor(x => x.PhotoBytes);
    }
}

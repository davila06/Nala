using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Infrastructure.Pets;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PawTrack.UnitTests.Pets.Services;

public sealed class WhatsAppAvatarComposerTests
{
    private readonly IBlobStorageService _blobStorage = Substitute.For<IBlobStorageService>();
    private readonly IQrCodeService _qrCode = Substitute.For<IQrCodeService>();

    [Fact]
    public async Task BuildAvatarAsync_WithPhotoAndQr_Returns640x640Png()
    {
        // Arrange
        var sourcePhoto = CreateSolidPng(1024, 768, new Rgba32(30, 144, 255));
        var qrBytes = CreateSolidPng(240, 240, new Rgba32(0, 0, 0));

        _blobStorage.DownloadAsync("https://cdn.test/photo.png", Arg.Any<CancellationToken>())
            .Returns(sourcePhoto);
        _qrCode.GeneratePng("https://pawtrack.cr/p/abc")
            .Returns(qrBytes);

        var sut = new WhatsAppAvatarComposer(_blobStorage, _qrCode);

        // Act
        var result = await sut.BuildAvatarAsync(
            sourcePhotoUrl: "https://cdn.test/photo.png",
            profileUrl: "https://pawtrack.cr/p/abc",
            petName: "Firulais",
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(1000);

        using var image = Image.Load<Rgba32>(result);
        image.Width.Should().Be(640);
        image.Height.Should().Be(640);
    }

    [Fact]
    public async Task BuildAvatarAsync_WithoutPhoto_UsesFallbackBackgroundAndStillReturnsPng()
    {
        // Arrange
        var qrBytes = CreateSolidPng(240, 240, new Rgba32(0, 0, 0));
        _qrCode.GeneratePng("https://pawtrack.cr/p/xyz")
            .Returns(qrBytes);

        var sut = new WhatsAppAvatarComposer(_blobStorage, _qrCode);

        // Act
        var result = await sut.BuildAvatarAsync(
            sourcePhotoUrl: null,
            profileUrl: "https://pawtrack.cr/p/xyz",
            petName: "Nala",
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        using var image = Image.Load<Rgba32>(result);
        image.Width.Should().Be(640);
        image.Height.Should().Be(640);
    }

    private static byte[] CreateSolidPng(int width, int height, Rgba32 color)
    {
        using var image = new Image<Rgba32>(width, height);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                row.Fill(color);
            }
        });

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }
}

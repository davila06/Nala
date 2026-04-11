using FluentAssertions;
using Microsoft.Extensions.Options;
using PawTrack.Application.Common.Settings;
using PawTrack.Infrastructure.Pets;

namespace PawTrack.UnitTests.Pets.Services;

/// <summary>
/// Verifies HmacAvatarTokenService generates verifiable tokens and rejects tampered or expired ones.
/// </summary>
public sealed class AvatarTokenServiceTests
{
    private static IOptions<AvatarTokenSettings> DefaultSettings() =>
        Options.Create(new AvatarTokenSettings
        {
            SigningKey = "test-secret-key-32-chars-minimum!!",
            ExpiryMinutes = 60,
        });

    [Fact]
    public void Generate_ReturnsNonEmptyToken()
    {
        var sut = new HmacAvatarTokenService(DefaultSettings());
        var petId = Guid.CreateVersion7();

        var token = sut.Generate(petId);

        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Validate_AcceptsJustGeneratedToken()
    {
        var sut = new HmacAvatarTokenService(DefaultSettings());
        var petId = Guid.CreateVersion7();

        var token = sut.Generate(petId);

        sut.Validate(petId, token).Should().BeTrue();
    }

    [Fact]
    public void Validate_RejectsTokenForDifferentPetId()
    {
        var sut = new HmacAvatarTokenService(DefaultSettings());
        var petId1 = Guid.CreateVersion7();
        var petId2 = Guid.CreateVersion7();

        var token = sut.Generate(petId1);

        sut.Validate(petId2, token).Should().BeFalse();
    }

    [Fact]
    public void Validate_RejectsTamperedToken()
    {
        var sut = new HmacAvatarTokenService(DefaultSettings());
        var petId = Guid.CreateVersion7();

        var token = sut.Generate(petId);
        var tampered = token[..^4] + "XXXX"; // corrupt last 4 chars

        sut.Validate(petId, tampered).Should().BeFalse();
    }

    [Fact]
    public void Validate_RejectsExpiredToken()
    {
        // Use a 0-minute expiry (already expired the instant it is generated)
        var settings = Options.Create(new AvatarTokenSettings
        {
            SigningKey = "test-secret-key-32-chars-minimum!!",
            ExpiryMinutes = 0,
        });
        var sut = new HmacAvatarTokenService(settings);
        var petId = Guid.CreateVersion7();

        var token = sut.Generate(petId);

        // Wait 1 tick to ensure expiry
        Thread.Sleep(1);

        sut.Validate(petId, token).Should().BeFalse();
    }

    [Fact]
    public void Validate_RejectsEmptyToken()
    {
        var sut = new HmacAvatarTokenService(DefaultSettings());
        var petId = Guid.CreateVersion7();

        sut.Validate(petId, string.Empty).Should().BeFalse();
        sut.Validate(petId, "   ").Should().BeFalse();
    }

    [Fact]
    public void Validate_RejectsMalformedToken()
    {
        var sut = new HmacAvatarTokenService(DefaultSettings());
        var petId = Guid.CreateVersion7();

        sut.Validate(petId, "not-a-valid-token-format").Should().BeFalse();
    }
}

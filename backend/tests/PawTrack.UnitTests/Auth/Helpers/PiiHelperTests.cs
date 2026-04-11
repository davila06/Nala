using FluentAssertions;
using PawTrack.Application.Common;

namespace PawTrack.UnitTests.Auth.Helpers;

/// <summary>
/// Round-6 security: email addresses must be masked before writing to logs.
/// </summary>
public sealed class PiiHelperTests
{
    [Theory]
    [InlineData("alice@example.com", "ali***@example.com")]
    [InlineData("bo@example.com", "bo***@example.com")]
    [InlineData("x@example.com", "x***@example.com")]
    [InlineData("longname@domain.org", "lon***@domain.org")]
    public void MaskEmail_ReturnsExpectedMaskedForm(string email, string expected)
    {
        PiiHelper.MaskEmail(email).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MaskEmail_NullOrWhitespace_ReturnsMaskOnly(string? email)
    {
        PiiHelper.MaskEmail(email).Should().Be("***");
    }

    [Fact]
    public void MaskEmail_NoAtSign_ReturnsMaskOnly()
    {
        PiiHelper.MaskEmail("invalidemail").Should().Be("***");
    }

    [Fact]
    public void MaskEmail_ResultDoesNotContainFullLocalPart()
    {
        var result = PiiHelper.MaskEmail("secretname@example.com");
        result.Should().NotContain("secretname",
            "the local part must be truncated to protect PII");
    }
}

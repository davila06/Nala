using FluentAssertions;
using PawTrack.Infrastructure.Sightings;

namespace PawTrack.UnitTests.Sightings.Services;

public sealed class PiiScrubberTests
{
    private readonly PiiScrubber _sut = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Scrub_NullOrWhitespace_ReturnsNull(string? input)
    {
        _sut.Scrub(input).Should().BeNull();
    }

    [Theory]
    [InlineData("He was running near the park", "He was running near the park")]
    [InlineData("Gray cat with a blue collar", "Gray cat with a blue collar")]
    public void Scrub_NoPii_ReturnsSameText(string input, string expected)
    {
        _sut.Scrub(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("Contact me at user@example.com for info", "Contact me at [REDACTED] for info")]
    [InlineData("Email john.doe+pets@subdomain.co.uk", "Email [REDACTED]")]
    public void Scrub_Email_RedactsEmail(string input, string expected)
    {
        _sut.Scrub(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("Call 8888-1234 if you find him", "Call [REDACTED] if you find him")]
    [InlineData("Costa Rica number +506 8888-1234", "Costa Rica number [REDACTED]")]
    public void Scrub_Phone_RedactsPhone(string input, string expected)
    {
        _sut.Scrub(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("Check https://maps.google.com/xyz for location", "Check [REDACTED] for location")]
    [InlineData("Visit www.example.com/sighting", "Visit [REDACTED]")]
    public void Scrub_Url_RedactsUrl(string input, string expected)
    {
        _sut.Scrub(input).Should().Be(expected);
    }

    [Fact]
    public void Scrub_MultipleTypes_RedactsAll()
    {
        const string input = "Found near park, call 8888-1234 or email me.owner@gmail.com";
        var result = _sut.Scrub(input);

        result.Should().NotContain("8888");
        result.Should().NotContain("me.owner@gmail.com");
        result.Should().Contain("[REDACTED]");
    }
}

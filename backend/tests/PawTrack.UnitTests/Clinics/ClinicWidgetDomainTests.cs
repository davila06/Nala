using FluentAssertions;
using PawTrack.Domain.Clinics;

namespace PawTrack.UnitTests.Clinics;

public sealed class ClinicWidgetDomainTests
{
    [Fact]
    public void Create_normalizes_domain_and_rejects_paths()
    {
        var domain = ClinicWidgetDomain.Create(Guid.NewGuid(), "HTTPS://Example.com/");

        domain.Domain.Should().Be("example.com");
    }

    [Theory]
    [InlineData("example.com/path")]
    [InlineData("http://")]
    [InlineData("")]
    public void Create_rejects_invalid_domains(string value)
    {
        var action = () => ClinicWidgetDomain.Create(Guid.NewGuid(), value);

        action.Should().Throw<ArgumentException>();
    }
}

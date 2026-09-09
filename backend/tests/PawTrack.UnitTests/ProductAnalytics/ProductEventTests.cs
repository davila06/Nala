using FluentAssertions;
using PawTrack.Domain.ProductAnalytics;

namespace PawTrack.UnitTests.ProductAnalytics;

public sealed class ProductEventTests
{
    [Fact]
    public void Create_TrimsBoundedIdentityFields()
    {
        var eventId = Guid.NewGuid();

        var result = ProductEvent.Create(
            eventId, " PetRegistered ", " 1 ", DateTimeOffset.UtcNow,
            " anonymous ", " create-pet ", canton: " San Jose ");

        result.EventId.Should().Be(eventId);
        result.EventName.Should().Be("PetRegistered");
        result.SchemaVersion.Should().Be("1");
        result.AnonymousId.Should().Be("anonymous");
        result.Source.Should().Be("create-pet");
        result.Canton.Should().Be("San Jose");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithoutRequiredText_Throws(string value)
    {
        var act = () => ProductEvent.Create(
            Guid.NewGuid(), value, "1", DateTimeOffset.UtcNow, "anon", "source");

        act.Should().Throw<ArgumentException>();
    }
}
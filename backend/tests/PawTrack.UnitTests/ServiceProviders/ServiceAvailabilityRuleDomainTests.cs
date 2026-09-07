using FluentAssertions;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class ServiceAvailabilityRuleDomainTests
{
    [Fact]
    public void Covers_IntervalWithinConfiguredDay_ReturnsTrue()
    {
        var rule = ServiceAvailabilityRule.Create(
            Guid.NewGuid(),
            DayOfWeek.Monday,
            new TimeOnly(9, 0),
            new TimeOnly(17, 0));
        var startsAt = new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.FromHours(-6));

        rule.Covers(startsAt, startsAt.AddMinutes(60)).Should().BeTrue();
        rule.Covers(startsAt.AddHours(6), startsAt.AddHours(8)).Should().BeFalse();
    }
}
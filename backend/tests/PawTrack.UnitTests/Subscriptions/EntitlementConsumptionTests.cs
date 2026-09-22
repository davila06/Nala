using FluentAssertions;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Subscriptions;

public sealed class EntitlementConsumptionTests
{
    [Fact]
    public void Create_rejects_non_positive_units()
    {
        var action = () => EntitlementConsumption.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "AiMatchesPerCycle",
            0m,
            "request-1",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMonths(1));

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_preserves_idempotency_and_cycle_context()
    {
        var subjectId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();
        var cycleStart = DateTimeOffset.UtcNow;
        var cycleEnd = cycleStart.AddMonths(1);

        var consumption = EntitlementConsumption.Create(
            subjectId,
            subscriptionId,
            "AiMatchesPerCycle",
            1m,
            "request-1",
            cycleStart,
            cycleEnd);

        consumption.SubjectId.Should().Be(subjectId);
        consumption.SubscriptionId.Should().Be(subscriptionId);
        consumption.EntitlementKey.Should().Be("AiMatchesPerCycle");
        consumption.Units.Should().Be(1m);
        consumption.IdempotencyKey.Should().Be("request-1");
        consumption.CycleStart.Should().Be(cycleStart);
        consumption.CycleEnd.Should().Be(cycleEnd);
    }
}

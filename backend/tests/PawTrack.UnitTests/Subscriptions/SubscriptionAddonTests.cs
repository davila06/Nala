using FluentAssertions;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Subscriptions;

public sealed class SubscriptionAddonTests
{
    [Fact]
    public void Create_requires_positive_units_and_valid_period()
    {
        var action = () => SubscriptionAddon.Create(
            Guid.NewGuid(), "MaxPets", 1m,
            DateTimeOffset.UtcNow.AddDays(2), DateTimeOffset.UtcNow);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_stores_vigencia_and_entitlement_key()
    {
        var startsAt = DateTimeOffset.UtcNow;
        var expiresAt = startsAt.AddMonths(1);
        var addon = SubscriptionAddon.Create(Guid.NewGuid(), "MaxPets", 1m, startsAt, expiresAt);

        addon.EntitlementKey.Should().Be("MaxPets");
        addon.Units.Should().Be(1m);
        addon.StartsAt.Should().Be(startsAt);
        addon.ExpiresAt.Should().Be(expiresAt);
        addon.IsActive.Should().BeTrue();
    }

    [Fact]
    public void RenewRecurring_extends_expiration_for_active_addon()
    {
        var startsAt = DateTimeOffset.UtcNow.AddDays(-10);
        var expiresAt = DateTimeOffset.UtcNow.AddDays(20);
        var addon = SubscriptionAddon.Create(Guid.NewGuid(), "MaxPets", 1m, startsAt, expiresAt);

        addon.RenewRecurring(1);

        addon.ExpiresAt.Should().BeCloseTo(expiresAt.AddMonths(1), TimeSpan.FromSeconds(1));
    }
}

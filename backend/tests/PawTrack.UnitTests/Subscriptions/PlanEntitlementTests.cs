using FluentAssertions;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Subscriptions;

public sealed class PlanEntitlementTests
{
    [Fact]
    public void Create_numeric_entitlement_requires_a_non_negative_limit()
    {
        var action = () => PlanEntitlement.Create(
            Guid.NewGuid(),
            "MaxPets",
            EntitlementValueType.Numeric,
            numericValue: -1,
            unit: "account");

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_boolean_entitlement_requires_boolean_value()
    {
        var action = () => PlanEntitlement.Create(
            Guid.NewGuid(),
            "CsvExportEnabled",
            EntitlementValueType.Boolean,
            booleanValue: null);

        action.Should().Throw<ArgumentException>();
    }
}

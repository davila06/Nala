using FluentAssertions;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.Subscriptions;

public sealed class DowngradeResourcePolicyTests
{
    [Fact]
    public void Published_provider_service_beyond_limit_is_paused()
    {
        DowngradeResourcePolicy.ShouldPauseProviderService(ProviderServiceStatus.Published, 25, 25).Should().BeTrue();
        DowngradeResourcePolicy.ShouldPauseProviderService(ProviderServiceStatus.Paused, 25, 25).Should().BeFalse();
    }

    [Fact]
    public void Primary_location_is_never_deactivated_by_downgrade()
    {
        DowngradeResourcePolicy.ShouldDeactivateStoreLocation(true, true, 5, 1).Should().BeFalse();
        DowngradeResourcePolicy.ShouldDeactivateStoreLocation(false, true, 5, 1).Should().BeTrue();
    }

    [Fact]
    public void Municipal_canton_is_removed_only_when_not_allowed()
    {
        DowngradeResourcePolicy.ShouldDropMunicipalCanton("Heredia", ["San Jose"]).Should().BeTrue();
        DowngradeResourcePolicy.ShouldDropMunicipalCanton("San Jose", ["San Jose"]).Should().BeFalse();
    }
}

using FluentAssertions;
using PawTrack.Domain.Auth;

namespace PawTrack.UnitTests.Auth.Domain;

public sealed class UserComplianceTests
{
    [Fact]
    public void Create_DefaultsIsAdultConfirmedToFalse()
    {
        var (user, _) = User.Create("user@test.com", "hash", "Test User");

        user.IsAdultConfirmed.Should().BeFalse();
    }

    [Fact]
    public void Create_WithIsAdultConfirmedTrue_PersistsFlag()
    {
        var (user, _) = User.Create("user@test.com", "hash", "Test User", isAdultConfirmed: true);

        user.IsAdultConfirmed.Should().BeTrue();
    }

    [Fact]
    public void Create_HasNoHealthDataConsent_ByDefault()
    {
        var (user, _) = User.Create("user@test.com", "hash", "Test User");

        user.HasHealthDataConsent.Should().BeFalse();
        user.HealthDataConsentedAt.Should().BeNull();
    }

    [Fact]
    public void GrantHealthDataConsent_SetsTimestampAndFlag()
    {
        var (user, _) = User.Create("user@test.com", "hash", "Test User");

        user.GrantHealthDataConsent();

        user.HasHealthDataConsent.Should().BeTrue();
        user.HealthDataConsentedAt.Should().NotBeNull();
    }

    [Fact]
    public void GrantHealthDataConsent_CalledTwice_DoesNotOverwriteOriginalTimestamp()
    {
        var (user, _) = User.Create("user@test.com", "hash", "Test User");
        user.GrantHealthDataConsent();
        var firstTimestamp = user.HealthDataConsentedAt;

        user.GrantHealthDataConsent();

        user.HealthDataConsentedAt.Should().Be(firstTimestamp);
    }
}

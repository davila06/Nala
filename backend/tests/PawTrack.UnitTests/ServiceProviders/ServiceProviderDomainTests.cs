using FluentAssertions;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class ServiceProviderDomainTests
{
    [Fact]
    public void Create_NormalizesProfileAndSetsPendingStatus()
    {
        var ownerUserId = Guid.NewGuid();

        var provider = ServiceProvider.Create(
            ownerUserId,
            "  Escuela Canina CR  ",
            "  Adiestramiento positivo  ",
            ServiceProviderCategory.Trainer,
            "  San Jose  ",
            9.9347m,
            -84.0875m,
            "  HOLA@EJEMPLO.CR  ");

        provider.UserId.Should().Be(ownerUserId);
        provider.Name.Should().Be("Escuela Canina CR");
        provider.Description.Should().Be("Adiestramiento positivo");
        provider.Category.Should().Be(ServiceProviderCategory.Trainer);
        provider.Address.Should().Be("San Jose");
        provider.ContactEmail.Should().Be("hola@ejemplo.cr");
        provider.Status.Should().Be(ServiceProviderStatus.Pending);
        provider.Id.Should().NotBeEmpty();
        provider.RegisteredAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        provider.MembershipTier.Should().Be(ProviderMembershipTier.Free);
        provider.TrialEndsAt.Should().BeNull();
        provider.HasCatalogAccess.Should().BeFalse();
    }

    [Fact]
    public void Activate_FirstApproval_StartsThirtyDayVerifiedTrial()
    {
        var provider = CreateProvider();

        provider.Activate();

        provider.MembershipTier.Should().Be(ProviderMembershipTier.Verified);
        provider.HasCatalogAccess.Should().BeTrue();
        provider.TrialEndsAt.Should().NotBeNull();
        provider.TrialEndsAt!.Value.Should().BeCloseTo(
            DateTimeOffset.UtcNow.AddDays(ServiceProvider.TrialDurationDays), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Activate_Reactivation_DoesNotRestartTrial()
    {
        var provider = CreateProvider();
        provider.Activate();
        var firstTrialEnd = provider.TrialEndsAt;
        provider.Suspend("motivo");

        provider.Activate();

        provider.TrialEndsAt.Should().Be(firstTrialEnd);
    }

    [Fact]
    public void ExpireTrialIfDue_BeforeExpiry_DoesNothing()
    {
        var provider = CreateProvider();
        provider.Activate();

        var changed = provider.ExpireTrialIfDue(DateTimeOffset.UtcNow);

        changed.Should().BeFalse();
        provider.MembershipTier.Should().Be(ProviderMembershipTier.Verified);
    }

    [Fact]
    public void ExpireTrialIfDue_AfterExpiry_DowngradesToFreeAndUnfeatures()
    {
        var provider = CreateProvider();
        provider.Activate();
        provider.SetFeatured(true);

        var changed = provider.ExpireTrialIfDue(DateTimeOffset.UtcNow.AddDays(ServiceProvider.TrialDurationDays + 1));

        changed.Should().BeTrue();
        provider.MembershipTier.Should().Be(ProviderMembershipTier.Free);
        provider.HasCatalogAccess.Should().BeFalse();
        provider.IsFeatured.Should().BeFalse();
    }

    [Fact]
    public void ExpireTrialIfDue_ManualMembership_NeverAutoDowngrades()
    {
        var provider = CreateProvider();
        provider.Activate();
        provider.SetMembership(ProviderMembershipTier.Featured, manual: true);

        var changed = provider.ExpireTrialIfDue(DateTimeOffset.UtcNow.AddYears(1));

        changed.Should().BeFalse();
        provider.MembershipTier.Should().Be(ProviderMembershipTier.Featured);
    }

    [Fact]
    public void SetMembership_ToFree_ClearsTrialEndsAt()
    {
        var provider = CreateProvider();
        provider.Activate();

        provider.SetMembership(ProviderMembershipTier.Free, manual: true);

        provider.MembershipTier.Should().Be(ProviderMembershipTier.Free);
        provider.TrialEndsAt.Should().BeNull();
        provider.IsMembershipManual.Should().BeTrue();
    }

    private static ServiceProvider CreateProvider() => ServiceProvider.Create(
        Guid.NewGuid(), "Grooming CR", "Cuidado profesional", ServiceProviderCategory.Groomer,
        "Heredia", 10m, -84m, "grooming@example.cr");
}
using FluentAssertions;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Subscriptions.Domain;

public sealed class SubscriptionDomainTests
{
    // ── CreateForUser ──────────────────────────────────────────────────────────

    [Fact]
    public void CreateForUser_ValidTier_SetsStatusToPendingPayment()
    {
        var sub = Subscription.CreateForUser(Guid.NewGuid(), SubscriptionTier.UserPlus, "ABCD1234", 2990m);

        sub.Status.Should().Be(SubscriptionStatus.PendingPayment);
        sub.UserId.Should().NotBeNull();
        sub.ClinicId.Should().BeNull();
        sub.IsActive.Should().BeFalse();
    }

    [Fact]
    public void CreateForUser_ClinicTier_Throws()
    {
        var act = () => Subscription.CreateForUser(Guid.NewGuid(), SubscriptionTier.ClinicPlus, "XXXXXXXX", 15000m);
        act.Should().Throw<ArgumentException>().WithMessage("*not a valid user tier*");
    }

    [Theory]
    [InlineData(SubscriptionTier.StorePlus)]
    [InlineData(SubscriptionTier.StorePartner)]
    public void CreateForUser_StoreTier_Succeeds(SubscriptionTier tier)
    {
        // Store owners are Auth.Users (Role = Store), so store subscriptions flow through
        // the generic user path — this must never regress back to throwing.
        var sub = Subscription.CreateForUser(Guid.NewGuid(), tier, "ABCD1234", 12000m);

        sub.Tier.Should().Be(tier);
        sub.Status.Should().Be(SubscriptionStatus.PendingPayment);
    }

    // ── CreateForClinic ────────────────────────────────────────────────────────

    [Fact]
    public void CreateForClinic_ValidTier_SetsClinicOwnerIdAndStatus()
    {
        var clinicId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var sub = Subscription.CreateForClinic(clinicId, ownerId, SubscriptionTier.ClinicPartner, "ABCD1234", 35000m);

        sub.ClinicId.Should().Be(clinicId);
        sub.ClinicOwnerId.Should().Be(ownerId);
        sub.UserId.Should().BeNull();
        sub.Status.Should().Be(SubscriptionStatus.PendingPayment);
    }

    [Fact]
    public void CreateForClinic_UserTier_Throws()
    {
        var act = () => Subscription.CreateForClinic(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.UserPlus, "XXXXXXXX", 2990m);
        act.Should().Throw<ArgumentException>().WithMessage("*not a valid clinic tier*");
    }

    // ── Activate ──────────────────────────────────────────────────────────────

    [Fact]
    public void Activate_FromPending_SetsActiveAndExpiry()
    {
        var sub = Subscription.CreateForUser(Guid.NewGuid(), SubscriptionTier.UserPlus, "ABCD1234", 2990m);
        sub.Activate();

        sub.Status.Should().Be(SubscriptionStatus.Active);
        sub.ActivatedAt.Should().NotBeNull();
        sub.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMonths(1), TimeSpan.FromSeconds(5));
        sub.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Activate_AlreadyActive_Throws()
    {
        var sub = Subscription.CreateForUser(Guid.NewGuid(), SubscriptionTier.UserPlus, "ABCD1234", 2990m);
        sub.Activate();

        var act = () => sub.Activate();
        act.Should().Throw<InvalidOperationException>();
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_ActiveSubscription_SetsStatusCancelled()
    {
        var sub = Subscription.CreateForUser(Guid.NewGuid(), SubscriptionTier.UserPlus, "ABCD1234", 2990m);
        sub.Activate();
        sub.Cancel();

        sub.Status.Should().Be(SubscriptionStatus.Cancelled);
        sub.CancelledAt.Should().NotBeNull();
        sub.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Cancel_PendingSubscription_Throws()
    {
        var sub = Subscription.CreateForUser(Guid.NewGuid(), SubscriptionTier.UserPlus, "ABCD1234", 2990m);
        var act = () => sub.Cancel();
        act.Should().Throw<InvalidOperationException>().WithMessage("*Only active*");
    }
}

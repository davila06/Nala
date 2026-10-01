using FluentAssertions;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Subscriptions.Domain;

public sealed class SubscriptionPlanApprovalTests
{
    [Fact]
    public void NewPlan_IsNotCommerciallyApproved()
    {
        var plan = SubscriptionPlan.Create(
            SubscriptionTier.UserPlus,
            "Plus",
            "Plan Plus",
            3000m,
            null);

        var approvalProperty = typeof(SubscriptionPlan).GetProperty("IsCommerciallyApproved");

        approvalProperty.Should().NotBeNull();
        approvalProperty!.GetValue(plan).Should().Be(false);
    }

    [Fact]
    public void ApproveForCommercialPublication_RequiresReferenceAndRecordsApprover()
    {
        var plan = CreatePlan();
        var approverId = Guid.NewGuid();

        plan.ApproveForCommercialPublication(approverId, "LEGAL-APPROVAL-2026-10");

        plan.IsCommerciallyApproved.Should().BeTrue();
        plan.CommercialApprovalReference.Should().Be("LEGAL-APPROVAL-2026-10");
        plan.CommercialApprovedByUserId.Should().Be(approverId);
        plan.CommercialApprovedAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("AB name")]
    public void ApproveForCommercialPublication_RejectsMissingReference(string reference)
    {
        var plan = CreatePlan();

        var act = () => plan.ApproveForCommercialPublication(Guid.NewGuid(), reference);

        act.Should().Throw<ArgumentException>();
        plan.IsCommerciallyApproved.Should().BeFalse();
    }

    [Fact]
    public void Update_RevokesCommercialApproval()
    {
        var plan = CreateApprovedPlan();

        plan.Update("Updated Plus", "Updated description", 3000m, null);

        plan.IsCommerciallyApproved.Should().BeFalse();
        plan.CommercialApprovalReference.Should().BeNull();
        plan.CommercialApprovedByUserId.Should().BeNull();
        plan.CommercialApprovedAt.Should().BeNull();
    }

    [Fact]
    public void Deactivate_RevokesCommercialApproval()
    {
        var plan = CreateApprovedPlan();

        plan.Deactivate();

        plan.IsActive.Should().BeFalse();
        plan.IsCommerciallyApproved.Should().BeFalse();
    }

    private static SubscriptionPlan CreatePlan() =>
        SubscriptionPlan.Create(SubscriptionTier.UserPlus, "Plus", "Plan Plus", 3000m, null);

    private static SubscriptionPlan CreateApprovedPlan()
    {
        var plan = CreatePlan();
        plan.ApproveForCommercialPublication(Guid.NewGuid(), "APPROVAL-REF");
        return plan;
    }
}

using FluentAssertions;
using PawTrack.Domain.Bounties;

namespace PawTrack.UnitTests.Bounties.Domain;

public sealed class BountyDomainTests
{
    private static Bounty MakeActive()
    {
        var b = Bounty.Create(Guid.NewGuid(), Guid.NewGuid(), 25_000m, "ABCD1234");
        b.ConfirmDeposit();
        return b;
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidAmount_SetsPendingDepositStatus()
    {
        var b = Bounty.Create(Guid.NewGuid(), Guid.NewGuid(), 10_000m, "ABCD1234");

        b.Status.Should().Be(BountyStatus.PendingDeposit);
        b.Amount.Should().Be(10_000m);
        b.PlatformFee.Should().Be(0.10m);
        b.NetPayoutAmount.Should().Be(9_000m);
        b.DepositReference.Should().Be("ABCD1234");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    public void Create_NonPositiveAmount_Throws(decimal amount)
    {
        var act = () => Bounty.Create(Guid.NewGuid(), Guid.NewGuid(), amount, "ABCD1234");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(0.51)]
    public void Create_InvalidPlatformFee_Throws(decimal fee)
    {
        var act = () => Bounty.Create(Guid.NewGuid(), Guid.NewGuid(), 10_000m, "ABCD1234", platformFee: fee);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ── ConfirmDeposit ────────────────────────────────────────────────────────

    [Fact]
    public void ConfirmDeposit_FromPending_SetsActive()
    {
        var b = Bounty.Create(Guid.NewGuid(), Guid.NewGuid(), 10_000m, "ABCD1234");
        b.ConfirmDeposit();

        b.Status.Should().Be(BountyStatus.Active);
        b.DepositedAt.Should().NotBeNull();
    }

    [Fact]
    public void ConfirmDeposit_AlreadyActive_Throws()
    {
        var b = MakeActive();
        var act = () => b.ConfirmDeposit();
        act.Should().Throw<InvalidOperationException>().WithMessage("*already confirmed*");
    }

    // ── Claim ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Claim_ActiveBounty_SetsClaimed()
    {
        var b = MakeActive();
        var sightingId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        b.Claim(sightingId, userId);

        b.Status.Should().Be(BountyStatus.Claimed);
        b.ClaimedBySightingId.Should().Be(sightingId);
        b.ClaimedByUserId.Should().Be(userId);
        b.ClaimedAt.Should().NotBeNull();
    }

    [Fact]
    public void Claim_PendingBounty_Throws()
    {
        var b = Bounty.Create(Guid.NewGuid(), Guid.NewGuid(), 10_000m, "ABCD1234");
        var act = () => b.Claim(Guid.NewGuid(), Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>().WithMessage("*Only active*");
    }

    // ── Release ───────────────────────────────────────────────────────────────

    [Fact]
    public void Release_ClaimedBounty_SetsReleased()
    {
        var b = MakeActive();
        b.Claim(Guid.NewGuid(), Guid.NewGuid());
        b.Release();

        b.Status.Should().Be(BountyStatus.Released);
        b.ReleasedAt.Should().NotBeNull();
    }

    [Fact]
    public void Release_ActiveBounty_Throws()
    {
        var b = MakeActive();
        var act = () => b.Release();
        act.Should().Throw<InvalidOperationException>().WithMessage("*Claimed*");
    }

    // ── Expire ────────────────────────────────────────────────────────────────

    [Fact]
    public void Expire_ActiveBounty_SetsExpired()
    {
        var b = MakeActive();
        b.Expire();
        b.Status.Should().Be(BountyStatus.Expired);
    }

    [Fact]
    public void Expire_ReleasedBounty_NoOp()
    {
        var b = MakeActive();
        b.Claim(Guid.NewGuid(), Guid.NewGuid());
        b.Release();
        b.Expire(); // should be a no-op

        b.Status.Should().Be(BountyStatus.Released);
    }
}

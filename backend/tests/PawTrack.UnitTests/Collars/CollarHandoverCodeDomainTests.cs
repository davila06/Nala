using FluentAssertions;
using PawTrack.Domain.Collars;

namespace PawTrack.UnitTests.Collars;

public sealed class CollarHandoverCodeDomainTests
{
    private static CollarHandoverCode MakeCode() =>
        CollarHandoverCode.Create(Guid.NewGuid(), Guid.NewGuid(), "hash123");

    [Fact]
    public void Create_DefaultsToRedeemable()
    {
        var code = MakeCode();

        code.IsRedeemable.Should().BeTrue();
        code.AttemptCount.Should().Be(0);
        code.ExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void RecordFailedAttempt_IncrementsCount()
    {
        var code = MakeCode();

        code.RecordFailedAttempt();

        code.AttemptCount.Should().Be(1);
    }

    [Fact]
    public void RecordFailedAttempt_ReachingMax_LocksCode()
    {
        var code = MakeCode();

        for (var i = 0; i < CollarHandoverCode.MaxAttempts; i++)
            code.RecordFailedAttempt();

        code.IsLocked.Should().BeTrue();
        code.IsRedeemable.Should().BeFalse();
    }

    [Fact]
    public void Redeem_SetsRedeemedAtAndUser()
    {
        var code = MakeCode();
        var newOwnerId = Guid.NewGuid();

        code.Redeem(newOwnerId);

        code.IsRedeemed.Should().BeTrue();
        code.RedeemedByUserId.Should().Be(newOwnerId);
        code.IsRedeemable.Should().BeFalse();
    }

    [Fact]
    public void Cancel_SetsCancelledAt()
    {
        var code = MakeCode();

        code.Cancel();

        code.IsCancelled.Should().BeTrue();
        code.IsRedeemable.Should().BeFalse();
    }
}

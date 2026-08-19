using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Bounties.Commands.ClaimBounty;
using PawTrack.Application.Bounties.Commands.ConfirmBountyDeposit;
using PawTrack.Application.Bounties.Commands.CreateBounty;
using PawTrack.Application.Bounties.Commands.ReleaseBounty;
using PawTrack.Application.Bounties.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Bounties;

namespace PawTrack.UnitTests.Bounties;

// ── Bounty domain ─────────────────────────────────────────────────────────────

public sealed class BountyDomainTests
{
    [Fact]
    public void Create_ValidAmount_CreatesPendingDeposit()
    {
        var b = Bounty.Create(Guid.NewGuid(), Guid.NewGuid(), 10_000m, "REF001");
        b.Status.Should().Be(BountyStatus.PendingDeposit);
        b.Amount.Should().Be(10_000m);
    }

    [Fact]
    public void ConfirmDeposit_FromPending_ActivatesBounty()
    {
        var b = Bounty.Create(Guid.NewGuid(), Guid.NewGuid(), 10_000m, "REF001");
        b.ConfirmDeposit();
        b.Status.Should().Be(BountyStatus.Active);
        b.DepositedAt.Should().NotBeNull();
    }

    [Fact]
    public void ConfirmDeposit_Twice_Throws()
    {
        var b = Bounty.Create(Guid.NewGuid(), Guid.NewGuid(), 10_000m, "REF001");
        b.ConfirmDeposit();
        var act = () => b.ConfirmDeposit();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Claim_FromActive_Transitions()
    {
        var b = Bounty.Create(Guid.NewGuid(), Guid.NewGuid(), 10_000m, "REF001");
        b.ConfirmDeposit();
        var sightingId = Guid.NewGuid();
        var claimant = Guid.NewGuid();
        b.Claim(sightingId, claimant);
        b.Status.Should().Be(BountyStatus.Claimed);
        b.ClaimedByUserId.Should().Be(claimant);
        b.ClaimedBySightingId.Should().Be(sightingId);
    }

    [Fact]
    public void Claim_WithNullSightingId_StoresNull()
    {
        var b = Bounty.Create(Guid.NewGuid(), Guid.NewGuid(), 10_000m, "REF001");
        b.ConfirmDeposit();
        b.Claim(null, Guid.NewGuid());
        b.ClaimedBySightingId.Should().BeNull();
    }

    [Fact]
    public void Claim_FromPending_Throws()
    {
        var b = Bounty.Create(Guid.NewGuid(), Guid.NewGuid(), 10_000m, "REF001");
        var act = () => b.Claim(null, Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Release_FromClaimed_Transitions()
    {
        var b = Bounty.Create(Guid.NewGuid(), Guid.NewGuid(), 10_000m, "REF001");
        b.ConfirmDeposit();
        b.Claim(null, Guid.NewGuid());
        b.Release();
        b.Status.Should().Be(BountyStatus.Released);
        b.ReleasedAt.Should().NotBeNull();
    }

    [Fact]
    public void Release_FromActive_Throws()
    {
        var b = Bounty.Create(Guid.NewGuid(), Guid.NewGuid(), 10_000m, "REF001");
        b.ConfirmDeposit();
        var act = () => b.Release();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void NetPayoutAmount_ReflectsPlatformFee()
    {
        var b = Bounty.Create(Guid.NewGuid(), Guid.NewGuid(), 10_000m, "REF", platformFee: 0.10m);
        b.NetPayoutAmount.Should().Be(9_000m);
    }

    [Fact]
    public void Expire_WhenActive_Transitions()
    {
        var b = Bounty.Create(Guid.NewGuid(), Guid.NewGuid(), 10_000m, "REF001");
        b.ConfirmDeposit();
        b.Expire();
        b.Status.Should().Be(BountyStatus.Expired);
    }

    [Fact]
    public void Expire_WhenReleased_IsNoOp()
    {
        var b = Bounty.Create(Guid.NewGuid(), Guid.NewGuid(), 10_000m, "REF001");
        b.ConfirmDeposit();
        b.Claim(null, Guid.NewGuid());
        b.Release();
        b.Expire(); // should not throw or change status
        b.Status.Should().Be(BountyStatus.Released);
    }
}

// ── CreateBountyCommandHandler ────────────────────────────────────────────────

public sealed class CreateBountyCommandHandlerTests
{
    private readonly IBountyRepository _repo = Substitute.For<IBountyRepository>();
    private readonly IPaymentService _payment = Substitute.For<IPaymentService>();
    private readonly ISubscriptionService _subs = Substitute.For<ISubscriptionService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_PlusUser_CreatesBounty()
    {
        _payment.GenerateReference().Returns("REF001");
        _subs.IsAtLeastPlusAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        _repo.GetByLostEventAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Bounty?)null);
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var sut = new CreateBountyCommandHandler(_repo, _payment, _subs, _uow);
        var result = await sut.Handle(
            new CreateBountyCommand(Guid.NewGuid(), Guid.NewGuid(), 15_000m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(15_000m);
    }

    [Fact]
    public async Task Handle_FreeUser_ReturnsFailure()
    {
        _subs.IsAtLeastPlusAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var sut = new CreateBountyCommandHandler(_repo, _payment, _subs, _uow);
        var result = await sut.Handle(
            new CreateBountyCommand(Guid.NewGuid(), Guid.NewGuid(), 15_000m), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(4_999)]
    [InlineData(0)]
    public async Task Handle_BelowMinAmount_ValidationFails(decimal amount)
    {
        var validator = new CreateBountyCommandValidator();
        var result = await validator.ValidateAsync(
            new CreateBountyCommand(Guid.NewGuid(), Guid.NewGuid(), amount));
        result.IsValid.Should().BeFalse();
    }
}

// ── ReleaseBountyCommandHandler ───────────────────────────────────────────────

public sealed class ReleaseBountyCommandHandlerTests
{
    private readonly IBountyRepository _repo = Substitute.For<IBountyRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_OwnerReleasesClaimed_Succeeds()
    {
        var ownerId = Guid.NewGuid();
        var bounty = Bounty.Create(Guid.NewGuid(), ownerId, 10_000m, "REF");
        bounty.ConfirmDeposit();
        bounty.Claim(null, Guid.NewGuid());

        _repo.GetByIdAsync(bounty.Id, Arg.Any<CancellationToken>()).Returns(bounty);
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var sut = new ReleaseBountyCommandHandler(_repo, _uow);
        var result = await sut.Handle(new ReleaseBountyCommand(bounty.Id, ownerId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        bounty.Status.Should().Be(BountyStatus.Released);
    }

    [Fact]
    public async Task Handle_NonOwner_ReturnsFailure()
    {
        var ownerId = Guid.NewGuid();
        var bounty = Bounty.Create(Guid.NewGuid(), ownerId, 10_000m, "REF");
        bounty.ConfirmDeposit();
        bounty.Claim(null, Guid.NewGuid());

        _repo.GetByIdAsync(bounty.Id, Arg.Any<CancellationToken>()).Returns(bounty);

        var sut = new ReleaseBountyCommandHandler(_repo, _uow);
        var result = await sut.Handle(
            new ReleaseBountyCommand(bounty.Id, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }
}

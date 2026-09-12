using FluentAssertions;
using MediatR;
using NSubstitute;
using PawTrack.Application.Bounties.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.Commands.ChargeCard;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Bounties;
using PawTrack.Domain.Payments;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Payments.Commands;

public sealed class ChargeCardCommandHandlerTests
{
    private readonly IUserPaymentProfileRepository _profileRepo = Substitute.For<IUserPaymentProfileRepository>();
    private readonly IPaymentTransactionRepository _transactionRepo = Substitute.For<IPaymentTransactionRepository>();
    private readonly IPaymentGatewayService _gatewayService = Substitute.For<IPaymentGatewayService>();
    private readonly ISubscriptionRepository _subscriptionRepo = Substitute.For<ISubscriptionRepository>();
    private readonly IBountyRepository _bountyRepo = Substitute.For<IBountyRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IElectronicBillingService _billingService = Substitute.For<IElectronicBillingService>();
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private ChargeCardCommandHandler CreateSut() =>
        new(_profileRepo, _transactionRepo, _gatewayService, _subscriptionRepo, _bountyRepo, _userRepo, _billingService, _sender, _unitOfWork);

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailure()
    {
        _userRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var sut = CreateSut();
        var result = await sut.Handle(
            new ChargeCardCommand(Guid.NewGuid(), 2990m, "Subscription", TransientToken: "tok_1"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Usuario no encontrado.");
    }

    [Fact]
    public async Task Handle_WhenValidTransientToken_ChargesAndActivatesSubscription()
    {
        var (user, _) = User.Create("owner@pawtrack.cr", "hash", "Test User");
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var sub = Subscription.CreateForUser(user.Id, SubscriptionTier.UserPlus, "REF12345", 2990m);
        _subscriptionRepo.GetByIdAsync(sub.Id, Arg.Any<CancellationToken>()).Returns(sub);

        _gatewayService.TokenizeTransientTokenAsync(Arg.Any<TokenizePaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TokenizePaymentResult(true, "cust_1", "instr_1", "Visa", "4242", 12, 2029, null));

        _gatewayService.ChargeAsync(Arg.Any<ChargePaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChargePaymentResult(true, "CS-TXN-1", "AUTH-123456", null, null));

        var sut = CreateSut();
        var result = await sut.Handle(
            new ChargeCardCommand(
                UserId: user.Id,
                AmountCrc: 2990m,
                Purpose: "Subscription",
                TargetEntityId: sub.Id,
                TransientToken: "transient_jwt_123",
                CardholderName: "Test User",
                SaveProfile: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Success.Should().BeTrue();
        result.Value.AuthorizationCode.Should().Be("AUTH-123456");
        result.Value.ActivatedSubscriptionId.Should().Be(sub.Id);

        sub.Status.Should().Be(SubscriptionStatus.Active);
        await _transactionRepo.Received(1).AddAsync(Arg.Any<PaymentTransaction>(), Arg.Any<CancellationToken>());
        await _profileRepo.Received(1).AddAsync(Arg.Any<UserPaymentProfile>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCardDeclined_RecordsFailureAndDoesNotActivateSubscription()
    {
        var (user, _) = User.Create("owner@pawtrack.cr", "hash", "Test User");
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var sub = Subscription.CreateForUser(user.Id, SubscriptionTier.UserPlus, "REF12345", 2990m);
        _subscriptionRepo.GetByIdAsync(sub.Id, Arg.Any<CancellationToken>()).Returns(sub);

        _gatewayService.TokenizeTransientTokenAsync(Arg.Any<TokenizePaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TokenizePaymentResult(true, "cust_1", "instr_1", "Visa", "4242", 12, 2029, null));

        _gatewayService.ChargeAsync(Arg.Any<ChargePaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChargePaymentResult(false, null, null, "INSUFFICIENT_FUNDS", "Fondos insuficientes"));

        var sut = CreateSut();
        var result = await sut.Handle(
            new ChargeCardCommand(
                UserId: user.Id,
                AmountCrc: 2990m,
                Purpose: "Subscription",
                TargetEntityId: sub.Id,
                TransientToken: "transient_jwt_123"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Success.Should().BeFalse();
        result.Value.ErrorMessage.Should().Contain("Fondos insuficientes");
        sub.Status.Should().Be(SubscriptionStatus.PendingPayment);
    }

    [Fact]
    public async Task Handle_WhenBountyPurpose_ChargesAndConfirmsBountyDeposit()
    {
        var (user, _) = User.Create("owner@pawtrack.cr", "hash", "Test User");
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var bounty = Bounty.Create(Guid.NewGuid(), user.Id, 25000m, "REFBOUNTY1");
        _bountyRepo.GetByIdAsync(bounty.Id, Arg.Any<CancellationToken>()).Returns(bounty);

        _gatewayService.TokenizeTransientTokenAsync(Arg.Any<TokenizePaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TokenizePaymentResult(true, "cust_1", "instr_1", "Visa", "4242", 12, 2029, null));

        _gatewayService.ChargeAsync(Arg.Any<ChargePaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChargePaymentResult(true, "CS-TXN-9", "AUTH-999111", null, null));

        var sut = CreateSut();
        var result = await sut.Handle(
            new ChargeCardCommand(
                UserId: user.Id,
                AmountCrc: 25000m,
                Purpose: "Bounty",
                TargetEntityId: bounty.Id,
                TransientToken: "transient_jwt_123"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Success.Should().BeTrue();
        result.Value.ConfirmedBountyId.Should().Be(bounty.Id);
        bounty.Status.Should().Be(BountyStatus.Active);
        _bountyRepo.Received(1).Update(bounty);
    }
}

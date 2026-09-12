using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Application.Subscriptions.Commands.CreateSubscription;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Payments;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Subscriptions.Commands;

public sealed class CreateSubscriptionCommandHandlerTests
{
    private readonly ISubscriptionRepository _subscriptionRepo = Substitute.For<ISubscriptionRepository>();
    private readonly ISubscriptionPlanRepository _planRepo = Substitute.For<ISubscriptionPlanRepository>();
    private readonly IPaymentService _paymentService = Substitute.For<IPaymentService>();
    private readonly IUserBillingProfileRepository _billingProfileRepo = Substitute.For<IUserBillingProfileRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public CreateSubscriptionCommandHandlerTests()
    {
        _paymentService.GenerateReference().Returns("REF12345");
    }

    private CreateSubscriptionCommandHandler CreateSut() =>
        new(_subscriptionRepo, _planRepo, _paymentService, _billingProfileRepo, _unitOfWork);

    [Fact]
    public async Task Handle_WhenRequiresInvoiceIsFalse_UsesBaseServiceCost()
    {
        var userId = Guid.NewGuid();
        var plan = SubscriptionPlan.Create(SubscriptionTier.UserPlus, "Plus", "Plan Plus", 2990m, null);
        _planRepo.GetByTierAsync(SubscriptionTier.UserPlus, Arg.Any<CancellationToken>()).Returns(plan);
        _subscriptionRepo.GetActiveForUserAsync(userId, Arg.Any<CancellationToken>()).Returns((Subscription?)null);

        var sut = CreateSut();
        var result = await sut.Handle(
            new CreateSubscriptionCommand(userId, null, userId, SubscriptionTier.UserPlus, 1, RequiresInvoice: false),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AmountCrc.Should().Be(2990m);
    }

    [Fact]
    public async Task Handle_WhenRequiresInvoiceIsTrue_AddsThirteenPercentIvaToServiceCost()
    {
        var userId = Guid.NewGuid();
        var plan = SubscriptionPlan.Create(SubscriptionTier.UserPlus, "Plus", "Plan Plus", 2990m, null);
        _planRepo.GetByTierAsync(SubscriptionTier.UserPlus, Arg.Any<CancellationToken>()).Returns(plan);
        _subscriptionRepo.GetActiveForUserAsync(userId, Arg.Any<CancellationToken>()).Returns((Subscription?)null);

        var sut = CreateSut();
        var result = await sut.Handle(
            new CreateSubscriptionCommand(userId, null, userId, SubscriptionTier.UserPlus, 1, RequiresInvoice: true),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // 2990 * 1.13 = 3378.70
        result.Value!.AmountCrc.Should().Be(3378.70m);
    }

    [Fact]
    public async Task Handle_WhenRequiresInvoiceNotSpecifiedButProfileRequiresInvoice_AddsThirteenPercentIva()
    {
        var userId = Guid.NewGuid();
        var plan = SubscriptionPlan.Create(SubscriptionTier.UserPlus, "Plus", "Plan Plus", 2990m, null);
        _planRepo.GetByTierAsync(SubscriptionTier.UserPlus, Arg.Any<CancellationToken>()).Returns(plan);
        _subscriptionRepo.GetActiveForUserAsync(userId, Arg.Any<CancellationToken>()).Returns((Subscription?)null);

        var profile = UserBillingProfile.Create(
            userId, TaxIdentificationType.Fisica, "101110222", "Juan Perez", "juan@test.cr",
            requiresInvoice: true);
        _billingProfileRepo.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);

        var sut = CreateSut();
        var result = await sut.Handle(
            new CreateSubscriptionCommand(userId, null, userId, SubscriptionTier.UserPlus, 1, RequiresInvoice: null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AmountCrc.Should().Be(3378.70m);
    }
}

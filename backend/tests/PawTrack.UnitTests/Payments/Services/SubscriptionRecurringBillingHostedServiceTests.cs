using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Payments;
using PawTrack.Domain.Subscriptions;
using PawTrack.Infrastructure.Payments;

namespace PawTrack.UnitTests.Payments.Services;

public sealed class SubscriptionRecurringBillingHostedServiceTests
{
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly IDistributedJobLock _jobLock = Substitute.For<IDistributedJobLock>();
    private readonly ISubscriptionRepository _subscriptionRepo = Substitute.For<ISubscriptionRepository>();
    private readonly IUserPaymentProfileRepository _profileRepo = Substitute.For<IUserPaymentProfileRepository>();
    private readonly IPaymentTransactionRepository _transactionRepo = Substitute.For<IPaymentTransactionRepository>();
    private readonly IPaymentGatewayService _gatewayService = Substitute.For<IPaymentGatewayService>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public SubscriptionRecurringBillingHostedServiceTests()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ISubscriptionRepository)).Returns(_subscriptionRepo);
        serviceProvider.GetService(typeof(IUserPaymentProfileRepository)).Returns(_profileRepo);
        serviceProvider.GetService(typeof(IPaymentTransactionRepository)).Returns(_transactionRepo);
        serviceProvider.GetService(typeof(IPaymentGatewayService)).Returns(_gatewayService);
        serviceProvider.GetService(typeof(IUserRepository)).Returns(_userRepo);
        serviceProvider.GetService(typeof(IEmailSender)).Returns(_emailSender);
        serviceProvider.GetService(typeof(IUnitOfWork)).Returns(_unitOfWork);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);
        _scopeFactory.CreateScope().Returns(scope);

        // Mock the distributed lock lease
        var lease = Substitute.For<IAsyncDisposable>();
        _jobLock.TryAcquireAsync("SubscriptionRecurringBilling", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(lease);
    }

    [Fact]
    public async Task RunCycleAsync_WhenSubscriptionExpiringAndHasSavedCard_RenewsAndSendsReceipt()
    {
        var (user, _) = User.Create("denis@pawtrack.cr", "hash", "Denis");
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var sub = Subscription.CreateForUser(user.Id, SubscriptionTier.UserPlus, "REF12345", 2990m, billingMonths: 1);
        sub.Activate(1);
        var initialExpiry = sub.ExpiresAt!.Value;

        _subscriptionRepo.GetExpiringWithinAsync(2, Arg.Any<CancellationToken>())
            .Returns([sub]);

        var card = UserPaymentProfile.CreateCard(user.Id, "tok_card_saved_1", "Visa", "1234", 11, 2030, isDefault: true);
        _profileRepo.GetDefaultByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(card);

        _gatewayService.ChargeAsync(Arg.Any<ChargePaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChargePaymentResult(true, "CS-REC-1", "AUTH-999000", null, null));

        var sut = new SubscriptionRecurringBillingHostedService(_scopeFactory, _jobLock, NullLogger<SubscriptionRecurringBillingHostedService>.Instance);
        await sut.RunCycleAsync(CancellationToken.None);

        sub.ExpiresAt.Should().BeAfter(initialExpiry);
        await _transactionRepo.Received(1).AddAsync(Arg.Any<PaymentTransaction>(), Arg.Any<CancellationToken>());
        await _emailSender.Received(1).SendRecurringPaymentReceiptAsync(
            user.Email, user.Name, Arg.Any<string>(), 2990m, "1234", sub.ExpiresAt.Value, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunCycleAsync_WhenChargeFails_RecordsFailureAndSendsAlert()
    {
        var (user, _) = User.Create("denis@pawtrack.cr", "hash", "Denis");
        _userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var sub = Subscription.CreateForUser(user.Id, SubscriptionTier.UserPlus, "REF12345", 2990m, billingMonths: 1);
        sub.Activate(1);

        _subscriptionRepo.GetExpiringWithinAsync(2, Arg.Any<CancellationToken>())
            .Returns([sub]);

        var card = UserPaymentProfile.CreateCard(user.Id, "tok_card_saved_1", "Visa", "1234", 11, 2030, isDefault: true);
        _profileRepo.GetDefaultByUserIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(card);

        _gatewayService.ChargeAsync(Arg.Any<ChargePaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChargePaymentResult(false, null, null, "CARD_EXPIRED", "Tarjeta vencida"));

        var sut = new SubscriptionRecurringBillingHostedService(_scopeFactory, _jobLock, NullLogger<SubscriptionRecurringBillingHostedService>.Instance);
        await sut.RunCycleAsync(CancellationToken.None);

        await _emailSender.Received(1).SendRecurringPaymentFailedAsync(
            user.Email, user.Name, Arg.Any<string>(), "Tarjeta vencida", Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }
}

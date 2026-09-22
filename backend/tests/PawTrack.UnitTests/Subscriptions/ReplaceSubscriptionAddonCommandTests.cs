using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Commands.ManageSubscriptionAddon;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Domain.Subscriptions;
using PawTrack.Domain.Payments;

namespace PawTrack.UnitTests.Subscriptions;

public sealed class ReplaceSubscriptionAddonCommandTests
{
    [Fact]
    public async Task Replace_deactivates_old_addon_and_returns_proration_credit()
    {
        var subscriptionId = Guid.NewGuid();
        var startsAt = DateTimeOffset.UtcNow.AddDays(-15);
        var expiresAt = DateTimeOffset.UtcNow.AddDays(15);
        var existing = SubscriptionAddon.Create(subscriptionId, "MaxPets", 1m, startsAt, expiresAt, 3000m);
        var repository = Substitute.For<ISubscriptionAddonRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var subscriptions = Substitute.For<ISubscriptionRepository>();
        var profiles = Substitute.For<IUserPaymentProfileRepository>();
        var transactions = Substitute.For<IPaymentTransactionRepository>();
        var gateway = Substitute.For<IPaymentGatewayService>();
        var userId = Guid.NewGuid();
        var subscription = Subscription.CreateForUser(userId, SubscriptionTier.UserPlus, "REF12345", 2990m);
        subscription.Activate();
        var profile = UserPaymentProfile.CreateCard(userId, "token", "Visa", "1234", 12, 2030, isDefault: true);
        subscriptions.GetByIdAsync(subscriptionId, Arg.Any<CancellationToken>()).Returns(subscription);
        profiles.GetDefaultByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);
        gateway.ChargeAsync(Arg.Any<ChargePaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChargePaymentResult(true, "GW-1", "AUTH-1", null, null));
        repository.GetByIdAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await new ReplaceSubscriptionAddonCommandHandler(
            repository, unitOfWork, subscriptions, profiles, transactions, gateway)
            .Handle(new ReplaceSubscriptionAddonCommand(
                existing.Id, "MaxPets", 2m, 4000m, DateTimeOffset.UtcNow.AddDays(30)), default);

        result.IsSuccess.Should().BeTrue();
        existing.IsActive.Should().BeFalse();
        result.Value!.Proration.UnusedCreditCrc.Should().BeGreaterThan(0m);
        await repository.Received(1).AddAsync(Arg.Is<SubscriptionAddon>(addon => addon.PriceCrc == 4000m), Arg.Any<CancellationToken>());
    }
}

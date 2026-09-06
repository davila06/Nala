using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Application.Subscriptions.Commands.AdminActivateSubscription;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Subscriptions.Commands;

public sealed class AdminActivateSubscriptionAnnualTests
{
    [Fact]
    public async Task Handle_MunicipalPlan_DefaultActivation_ExpiresAfterOneYear()
    {
        var subscription = Subscription.CreateForUser(
            Guid.NewGuid(),
            SubscriptionTier.MuniFull,
            "ABCD1234",
            300000m);
        var subscriptions = Substitute.For<ISubscriptionRepository>();
        subscriptions.GetByIdAsync(subscription.Id, Arg.Any<CancellationToken>())
            .Returns(subscription);
        var uow = Substitute.For<IUnitOfWork>();
        uow.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new AdminActivateSubscriptionCommandHandler(
            subscriptions,
            Substitute.For<IClinicRepository>(),
            Substitute.For<IStoreRepository>(),
            Substitute.For<IMunicipalProfileRepository>(),
            Substitute.For<IAuditLogRepository>(),
            uow);

        var before = DateTimeOffset.UtcNow.AddYears(1);
        var result = await handler.Handle(
            new AdminActivateSubscriptionCommand(subscription.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        subscription.ExpiresAt.Should().BeCloseTo(before, TimeSpan.FromSeconds(5));
    }
}

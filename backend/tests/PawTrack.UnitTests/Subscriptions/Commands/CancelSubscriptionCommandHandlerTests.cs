using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Commands.CancelSubscription;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Stores;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Subscriptions.Commands;

public sealed class CancelSubscriptionCommandHandlerTests
{
    private readonly ISubscriptionRepository _subscriptions = Substitute.For<ISubscriptionRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private CancelSubscriptionCommandHandler BuildHandler() =>
        new(_subscriptions, _uow);

    [Fact]
    public async Task Handle_ActiveSubscription_SchedulesCancellationWithoutRevokingAccess()
    {
        var store = Store.Create(Guid.NewGuid(), "PetShop CR", "desc", "San José", 9.9m, -84.0m, "a@b.com");
        var sub = Subscription.CreateForUser(store.UserId, SubscriptionTier.StorePartner, "ABCD1234", 25000m);
        sub.Activate();

        _subscriptions.GetByIdAsync(sub.Id, Arg.Any<CancellationToken>()).Returns(sub);
        var result = await BuildHandler().Handle(
            new CancelSubscriptionCommand(sub.Id, store.UserId), default);

        result.IsSuccess.Should().BeTrue();
        sub.Status.Should().Be(SubscriptionStatus.Active);
        sub.CancellationRequestedAt.Should().NotBeNull();
        sub.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WrongRequestingUser_ReturnsAccessDenied()
    {
        var sub = Subscription.CreateForUser(Guid.NewGuid(), SubscriptionTier.StorePlus, "ABCD1234", 12000m);
        sub.Activate();
        _subscriptions.GetByIdAsync(sub.Id, Arg.Any<CancellationToken>()).Returns(sub);

        var result = await BuildHandler().Handle(
            new CancelSubscriptionCommand(sub.Id, Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Access denied.");
    }

    [Fact]
    public async Task Handle_ClinicPartnerCancellation_DoesNotRevokeKeysBeforeExpiry()
    {
        var ownerId = Guid.NewGuid();
        var clinicId = Guid.NewGuid();
        var sub = Subscription.CreateForClinic(clinicId, ownerId, SubscriptionTier.ClinicPartner, "ABCD1234", 35000m);
        sub.Activate();

        _subscriptions.GetByIdAsync(sub.Id, Arg.Any<CancellationToken>()).Returns(sub);
        var result = await BuildHandler().Handle(new CancelSubscriptionCommand(sub.Id, ownerId), default);

        result.IsSuccess.Should().BeTrue();
        sub.IsActive.Should().BeTrue();
    }
}

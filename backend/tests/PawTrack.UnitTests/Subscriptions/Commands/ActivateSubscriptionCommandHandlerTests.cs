using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Application.Subscriptions.Commands.ActivateSubscription;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Stores;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Subscriptions.Commands;

public sealed class ActivateSubscriptionCommandHandlerTests
{
    private readonly ISubscriptionRepository _subscriptions = Substitute.For<ISubscriptionRepository>();
    private readonly IClinicRepository _clinics = Substitute.For<IClinicRepository>();
    private readonly IStoreRepository _stores = Substitute.For<IStoreRepository>();
    private readonly IMunicipalProfileRepository _municipal = Substitute.For<IMunicipalProfileRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private ActivateSubscriptionCommandHandler BuildHandler() =>
        new(_subscriptions, _clinics, _stores, _municipal, _uow);

    [Fact]
    public async Task Handle_UnknownReference_ReturnsFailure()
    {
        _subscriptions.GetByPaymentReferenceAsync("NOPE0000", Arg.Any<CancellationToken>())
            .Returns((Subscription?)null);

        var result = await BuildHandler().Handle(new ActivateSubscriptionCommand("NOPE0000"), default);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_StorePlusSubscription_ActivatesAndFeaturesTheStore()
    {
        var store = Store.Create(Guid.NewGuid(), "PetShop CR", "desc", "San José", 9.9m, -84.0m, "a@b.com");
        var sub = Subscription.CreateForUser(store.UserId, SubscriptionTier.StorePlus, "ABCD1234", 12000m);

        _subscriptions.GetByPaymentReferenceAsync("ABCD1234", Arg.Any<CancellationToken>()).Returns(sub);
        _stores.GetByUserIdAsync(store.UserId, Arg.Any<CancellationToken>()).Returns(store);

        var result = await BuildHandler().Handle(new ActivateSubscriptionCommand("ABCD1234"), default);

        result.IsSuccess.Should().BeTrue();
        sub.Status.Should().Be(SubscriptionStatus.Active);
        store.IsFeatured.Should().BeTrue();
        _stores.Received(1).Update(store);
    }

    [Fact]
    public async Task Handle_ClinicPartnerSubscription_ActivatesAndFeaturesTheClinic()
    {
        var clinic = Clinic.Create(Guid.NewGuid(), "VetSalud", "SEN-123", "Heredia", 10m, -84.1m, "vet@x.com");
        var sub = Subscription.CreateForClinic(clinic.Id, Guid.NewGuid(), SubscriptionTier.ClinicPartner, "EFGH5678", 35000m);

        _subscriptions.GetByPaymentReferenceAsync("EFGH5678", Arg.Any<CancellationToken>()).Returns(sub);
        _clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);

        var result = await BuildHandler().Handle(new ActivateSubscriptionCommand("EFGH5678"), default);

        result.IsSuccess.Should().BeTrue();
        clinic.IsFeatured.Should().BeTrue();
        _clinics.Received(1).Update(clinic);
        // Clinic subscriptions have no UserId, so store sync must be a no-op
        await _stores.DidNotReceive().GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}

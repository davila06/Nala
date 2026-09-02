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
    private readonly IClinicRepository _clinics = Substitute.For<IClinicRepository>();
    private readonly IStoreRepository _stores = Substitute.For<IStoreRepository>();
    private readonly IClinicApiKeyRepository _apiKeys = Substitute.For<IClinicApiKeyRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private CancelSubscriptionCommandHandler BuildHandler() =>
        new(_subscriptions, _clinics, _stores, _apiKeys, _uow);

    [Fact]
    public async Task Handle_ActiveStorePartnerSubscription_CancelsAndUnfeaturesTheStore()
    {
        var store = Store.Create(Guid.NewGuid(), "PetShop CR", "desc", "San José", 9.9m, -84.0m, "a@b.com");
        store.SetFeatured(true);
        var sub = Subscription.CreateForUser(store.UserId, SubscriptionTier.StorePartner, "ABCD1234", 25000m);
        sub.Activate();

        _subscriptions.GetByIdAsync(sub.Id, Arg.Any<CancellationToken>()).Returns(sub);
        _stores.GetByUserIdAsync(store.UserId, Arg.Any<CancellationToken>()).Returns(store);

        var result = await BuildHandler().Handle(
            new CancelSubscriptionCommand(sub.Id, store.UserId), default);

        result.IsSuccess.Should().BeTrue();
        sub.Status.Should().Be(SubscriptionStatus.Cancelled);
        store.IsFeatured.Should().BeFalse();
        _stores.Received(1).Update(store);
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
    public async Task Handle_ClinicPartnerCancelled_RevokesAllApiKeys()
    {
        var ownerId = Guid.NewGuid();
        var clinicId = Guid.NewGuid();
        var clinic = Clinic.Create(ownerId, "VetSalud", "SEN-1", "Heredia", 10m, -84m, "vet@x.com");
        clinic.SetFeatured(true);
        var sub = Subscription.CreateForClinic(clinicId, ownerId, SubscriptionTier.ClinicPartner, "ABCD1234", 35000m);
        sub.Activate();

        var key1 = ClinicApiKey.Create(clinicId, "hash1", "Key 1");
        var key2 = ClinicApiKey.Create(clinicId, "hash2", "Key 2");

        _subscriptions.GetByIdAsync(sub.Id, Arg.Any<CancellationToken>()).Returns(sub);
        _clinics.GetByIdAsync(clinicId, Arg.Any<CancellationToken>()).Returns(clinic);
        _apiKeys.GetForClinicAsync(clinicId, Arg.Any<CancellationToken>())
            .Returns(new List<ClinicApiKey> { key1, key2 });

        var result = await BuildHandler().Handle(new CancelSubscriptionCommand(sub.Id, ownerId), default);

        result.IsSuccess.Should().BeTrue();
        key1.IsRevoked.Should().BeTrue();
        key2.IsRevoked.Should().BeTrue();
        _apiKeys.Received(1).Update(key1);
        _apiKeys.Received(1).Update(key2);
    }
}

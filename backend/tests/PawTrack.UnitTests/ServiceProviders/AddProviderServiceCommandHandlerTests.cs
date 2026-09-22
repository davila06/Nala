using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.ServiceProviders;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class AddProviderServiceCommandHandlerTests
{
    [Fact]
    public async Task Handle_ProviderAtServiceLimit_ReturnsFailure()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var entitlements = Substitute.For<IEntitlementService>();
        var ownerUserId = Guid.NewGuid();
        var provider = ServiceProvider.Create(
            ownerUserId, "Escuela Canina", "Adiestramiento", ServiceProviderCategory.Trainer,
            "San Jose", 9.9m, -84m, "provider@example.cr");
        provider.Activate();
        repository.GetByUserIdAsync(ownerUserId, Arg.Any<CancellationToken>()).Returns(provider);
        repository.GetServicesByProviderAsync(provider.Id, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Range(1, 25).Select(_ => ProviderService.Create(
                provider.Id, "Servicio", "Descripcion", ServiceModality.AtProviderLocation, 60, 25_000m, 1)).ToList());
        entitlements.AuthorizeAsync(
            ownerUserId, "MaxActiveServices", 1m, Arg.Any<EntitlementContext>(), Arg.Any<CancellationToken>())
            .Returns(new EntitlementDecision(false, true, 25m, 25m, 0m, null, SubscriptionTier.UserPlus));

        var handler = new AddProviderServiceCommandHandler(repository, unitOfWork, entitlements);
        var result = await handler.Handle(new AddProviderServiceCommand(
            ownerUserId, "Nuevo", "Descripcion", ServiceModality.AtProviderLocation, 60, 25_000m, 1), default);

        result.IsFailure.Should().BeTrue();
        await repository.DidNotReceive().AddServiceAsync(Arg.Any<ProviderService>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProviderOnFreeTier_ReturnsFailure()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var ownerUserId = Guid.NewGuid();
        var provider = ServiceProvider.Create(
            ownerUserId, "Escuela Canina", "Adiestramiento", ServiceProviderCategory.Trainer,
            "San Jose", 9.9m, -84m, "provider@example.cr");
        provider.Activate();
        provider.SetMembership(ProviderMembershipTier.Free, manual: true);
        repository.GetByUserIdAsync(ownerUserId, Arg.Any<CancellationToken>()).Returns(provider);

        var handler = new AddProviderServiceCommandHandler(repository, unitOfWork);
        var result = await handler.Handle(new AddProviderServiceCommand(
            ownerUserId, "Sesion", "Individual", ServiceModality.AtProviderLocation, 60, 25_000m, 1), default);

        result.IsFailure.Should().BeTrue();
        await repository.DidNotReceive().AddServiceAsync(Arg.Any<ProviderService>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProviderOnVerifiedTrial_PublishesService()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var ownerUserId = Guid.NewGuid();
        var provider = ServiceProvider.Create(
            ownerUserId, "Escuela Canina", "Adiestramiento", ServiceProviderCategory.Trainer,
            "San Jose", 9.9m, -84m, "provider@example.cr");
        provider.Activate();
        repository.GetByUserIdAsync(ownerUserId, Arg.Any<CancellationToken>()).Returns(provider);

        var handler = new AddProviderServiceCommandHandler(repository, unitOfWork);
        var result = await handler.Handle(new AddProviderServiceCommand(
            ownerUserId, "Sesion", "Individual", ServiceModality.AtProviderLocation, 60, 25_000m, 1), default);

        result.IsSuccess.Should().BeTrue();
        await repository.Received(1).AddServiceAsync(Arg.Any<ProviderService>(), Arg.Any<CancellationToken>());
    }
}

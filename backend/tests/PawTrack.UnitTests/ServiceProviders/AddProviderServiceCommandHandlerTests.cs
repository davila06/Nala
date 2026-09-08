using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class AddProviderServiceCommandHandlerTests
{
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

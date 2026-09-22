using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Commands.ManageSubscriptionAddon;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Subscriptions;

public sealed class ManageSubscriptionAddonCommandTests
{
    [Fact]
    public async Task Create_missing_subscription_returns_failure_without_persisting()
    {
        var subscriptions = Substitute.For<ISubscriptionRepository>();
        var addons = Substitute.For<ISubscriptionAddonRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        subscriptions.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Subscription?)null);

        var result = await new CreateSubscriptionAddonCommandHandler(subscriptions, addons, unitOfWork)
            .Handle(new CreateSubscriptionAddonCommand(
                Guid.NewGuid(), "MaxPets", 1m,
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30)), default);

        result.IsFailure.Should().BeTrue();
        await addons.DidNotReceive().AddAsync(Arg.Any<SubscriptionAddon>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deactivate_marks_existing_addon_inactive()
    {
        var addon = SubscriptionAddon.Create(
            Guid.NewGuid(), "MaxPets", 1m,
            DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        var addons = Substitute.For<ISubscriptionAddonRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        addons.GetByIdAsync(addon.Id, Arg.Any<CancellationToken>()).Returns(addon);

        var result = await new DeactivateSubscriptionAddonCommandHandler(addons, unitOfWork)
            .Handle(new DeactivateSubscriptionAddonCommand(addon.Id), default);

        result.IsSuccess.Should().BeTrue();
        addon.IsActive.Should().BeFalse();
        addons.Received(1).Update(addon);
    }
}

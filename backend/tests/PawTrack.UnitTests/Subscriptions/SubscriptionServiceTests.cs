using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Domain.Subscriptions;
using PawTrack.Infrastructure.Subscriptions;

namespace PawTrack.UnitTests.Subscriptions;

public sealed class SubscriptionServiceTests
{
    [Fact]
    public async Task GetMonthlyAiSearchLimit_FreeWithoutEntitlementService_ReturnsZero()
    {
        var userId = Guid.NewGuid();
        var subscriptions = Substitute.For<ISubscriptionRepository>();
        subscriptions.GetActiveForUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns((Subscription?)null);
        var service = new SubscriptionService(subscriptions);

        var limit = await service.GetMonthlyAiSearchLimitAsync(userId);

        limit.Should().Be(0);
    }
}

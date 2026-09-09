using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class ProviderBookingQueryAuthorizationTests
{
    [Fact]
    public async Task IncomingBookings_QueriesOnlyTheAuthenticatedProvider()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var ownerId = Guid.NewGuid();
        var provider = ServiceProvider.Create(
            ownerId, "Provider", "Cuidado", ServiceProviderCategory.Other,
            "San Jose", 9.9m, -84m, "provider@example.cr");
        repository.GetByUserIdAsync(ownerId, Arg.Any<CancellationToken>()).Returns(provider);
        repository.GetBookingsByProviderAsync(provider.Id, 0, 50, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProviderBooking>());

        var handler = new GetIncomingProviderBookingsQueryHandler(repository);
        var result = await handler.Handle(new GetIncomingProviderBookingsQuery(ownerId, 1, 50), default);

        result.IsSuccess.Should().BeTrue();
        await repository.Received(1).GetBookingsByProviderAsync(provider.Id, 0, 50, Arg.Any<CancellationToken>());
        await repository.DidNotReceive().GetBookingsByProviderAsync(
            Arg.Is<Guid>(id => id != provider.Id), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}
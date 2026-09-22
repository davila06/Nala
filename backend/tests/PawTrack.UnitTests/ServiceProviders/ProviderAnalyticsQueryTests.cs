using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class ProviderAnalyticsQueryTests
{
    [Fact]
    public async Task Analytics_includes_bookings_beyond_the_first_page()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var ownerId = Guid.NewGuid();
        var provider = ServiceProvider.Create(
            ownerId, "Provider", "Cuidado", ServiceProviderCategory.Other,
            "San Jose", 9.9m, -84m, "provider@example.cr");
        provider.Activate();

        var firstPage = Enumerable.Range(0, 5_000)
            .Select(_ => CreateBooking(provider.Id))
            .ToList();
        var secondPage = new[] { CreateBooking(provider.Id) };

        repository.GetByUserIdAsync(ownerId, Arg.Any<CancellationToken>()).Returns(provider);
        repository.GetBookingsByProviderAsync(provider.Id, 0, 5_000, Arg.Any<CancellationToken>())
            .Returns(firstPage);
        repository.GetBookingsByProviderAsync(provider.Id, 5_000, 5_000, Arg.Any<CancellationToken>())
            .Returns(secondPage);
        repository.GetPublishedServicesByProviderAsync(provider.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ProviderService>());

        var now = DateTimeOffset.UtcNow;
        var handler = new GetProviderAnalyticsQueryHandler(repository);
        var result = await handler.Handle(
            new GetProviderAnalyticsQuery(ownerId, now.AddDays(-1), now.AddDays(1)),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalBookings.Should().Be(5_001);
        await repository.Received(1).GetBookingsByProviderAsync(provider.Id, 5_000, 5_000, Arg.Any<CancellationToken>());
    }

    private static ProviderBooking CreateBooking(Guid providerId) => ProviderBooking.Request(
        providerId,
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        "Consulta",
        DateTimeOffset.UtcNow.AddDays(2),
        30,
        10_000m,
        1,
        null);
}

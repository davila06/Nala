using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PawTrack.Domain.ServiceProviders;
using PawTrack.Infrastructure.Persistence;
using PawTrack.Infrastructure.ServiceProviders;

namespace PawTrack.IntegrationTests.ServiceProviders;

[Collection("Integration")]
public sealed class ProviderBookingSqlConcurrencyTests
{
    private const string ConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=PawTrackDev;Integrated Security=True;TrustServerCertificate=True;";

    [Fact]
    public async Task TryAddBookingAsync_ConcurrentRequestsForCapacityOne_AllowsExactlyOne()
    {
        var options = new DbContextOptionsBuilder<PawTrackDbContext>()
            .UseSqlServer(ConnectionString, sqlServer =>
            {
                sqlServer.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
                sqlServer.UseNetTopologySuite();
            })
            .Options;
        var providerId = Guid.CreateVersion7();
        var service = ProviderService.Create(providerId, "Concurrency test", "Test", ServiceModality.AtProviderLocation, 60, 1m, 1);
        var startsAt = DateTimeOffset.UtcNow.AddDays(7).AddMinutes(-DateTimeOffset.UtcNow.Minute).AddSeconds(-DateTimeOffset.UtcNow.Second);

        await using (var setup = new PawTrackDbContext(options))
        {
            await setup.ProviderServices.AddAsync(service);
            await setup.SaveChangesAsync();
        }

        try
        {
            await using var firstContext = new PawTrackDbContext(options);
            await using var secondContext = new PawTrackDbContext(options);
            var first = ProviderBooking.Request(providerId, service.Id, Guid.CreateVersion7(), Guid.CreateVersion7(), service.Name, startsAt, 60, 1m, 1, null);
            var second = ProviderBooking.Request(providerId, service.Id, Guid.CreateVersion7(), Guid.CreateVersion7(), service.Name, startsAt, 60, 1m, 1, null);

            var results = await Task.WhenAll(
                new ServiceProviderRepository(firstContext).TryAddBookingAsync(first, 1),
                new ServiceProviderRepository(secondContext).TryAddBookingAsync(second, 1));

            results.Count(result => result).Should().Be(1);
        }
        finally
        {
            await using var cleanup = new PawTrackDbContext(options);
            var bookings = await cleanup.ProviderBookings.Where(booking => booking.ProviderServiceId == service.Id).ToListAsync();
            cleanup.ProviderBookings.RemoveRange(bookings);
            cleanup.ProviderServices.Remove(service);
            await cleanup.SaveChangesAsync();
        }
    }
}
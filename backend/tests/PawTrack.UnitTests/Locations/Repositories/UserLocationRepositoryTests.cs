using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PawTrack.Domain.Locations;
using PawTrack.Infrastructure.Locations;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.UnitTests.Locations.Repositories;

public sealed class UserLocationRepositoryTests
{
    [Fact]
    public async Task UpsertAsync_DetachedEntity_MarksAsAdded()
    {
        var options = new DbContextOptionsBuilder<PawTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new PawTrackDbContext(options);
        var sut = new UserLocationRepository(dbContext);

        var location = UserLocation.Create(
            Guid.NewGuid(),
            9.934739,
            -84.087502,
            receiveNearbyAlerts: true,
            quietHoursStart: new TimeOnly(22, 0),
            quietHoursEnd: new TimeOnly(6, 0));

        await sut.UpsertAsync(location, CancellationToken.None);

        dbContext.Entry(location).State.Should().Be(EntityState.Added);
    }
}

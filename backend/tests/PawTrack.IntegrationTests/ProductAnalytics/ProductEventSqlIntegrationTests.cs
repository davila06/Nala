using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PawTrack.Domain.ProductAnalytics;
using PawTrack.Infrastructure.Persistence;
using PawTrack.Infrastructure.ProductAnalytics;

namespace PawTrack.IntegrationTests.ProductAnalytics;

[Collection("Integration")]
public sealed class ProductEventSqlIntegrationTests
{
    private const string ConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=PawTrackDev;Integrated Security=True;TrustServerCertificate=True;";

    [Fact]
    public async Task PerformanceByCohort_GroupsSeparateLostIncidentsAndCalculatesMedian()
    {
        var options = CreateOptions();
        var petId = Guid.CreateVersion7();
        var firstLostEventId = $"sql-test-{Guid.NewGuid():N}";
        var secondLostEventId = $"sql-test-{Guid.NewGuid():N}";
        var from = DateTimeOffset.UtcNow.AddMinutes(-30);
        var to = DateTimeOffset.UtcNow.AddMinutes(5);

        await using (var setup = new PawTrackDbContext(options))
        {
            await setup.ProductEvents.AddRangeAsync(
                CreateEvent("LostPetReported", firstLostEventId, petId, from.AddMinutes(1)),
                CreateEvent("FirstResponseRecorded", firstLostEventId, petId, from.AddMinutes(2)),
                CreateEvent("LostPetReported", secondLostEventId, petId, from.AddMinutes(3)),
                CreateEvent("FirstResponseRecorded", secondLostEventId, petId, from.AddMinutes(7)));
            await setup.SaveChangesAsync();
        }

        try
        {
            var repository = new ProductEventRepository(new PawTrackDbContext(options));
            var metrics = await repository.GetPerformanceByCohortAsync(
                from.AddMinutes(-1), to, "San José");

            var metric = metrics.Should().ContainSingle().Subject;
            metric.LostReports.Should().Be(2);
            metric.ReunitedReports.Should().Be(0);
            metric.MedianFirstResponseMinutes.Should().BeApproximately(2.5, 0.01);
        }
        finally
        {
            await using var cleanup = new PawTrackDbContext(options);
            await cleanup.ProductEvents
                .Where(e => e.CorrelationId == firstLostEventId || e.CorrelationId == secondLostEventId)
                .ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task FirstResponseIndex_AllowsOnlyOneEventPerIncident()
    {
        var options = CreateOptions();
        var correlationId = $"sql-test-{Guid.NewGuid():N}";

        try
        {
            await using var first = new PawTrackDbContext(options);
            await first.ProductEvents.AddAsync(CreateEvent(
                "FirstResponseRecorded", correlationId, Guid.CreateVersion7(), DateTimeOffset.UtcNow));
            await first.SaveChangesAsync();

            await using var duplicate = new PawTrackDbContext(options);
            await duplicate.ProductEvents.AddAsync(CreateEvent(
                "FirstResponseRecorded", correlationId, Guid.CreateVersion7(), DateTimeOffset.UtcNow));

            var act = () => duplicate.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateException>();
        }
        finally
        {
            await using var cleanup = new PawTrackDbContext(options);
            await cleanup.ProductEvents
                .Where(e => e.CorrelationId == correlationId)
                .ExecuteDeleteAsync();
        }
    }

    private static ProductEvent CreateEvent(
        string eventName,
        string correlationId,
        Guid petId,
        DateTimeOffset occurredAt) => ProductEvent.Create(
            Guid.CreateVersion7(),
            eventName,
            "1",
            occurredAt,
            $"sql-test-{correlationId}",
            "integration-test",
            petId: petId,
            canton: "San José",
            correlationId: correlationId,
            receivedAt: occurredAt);

    private static DbContextOptions<PawTrackDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<PawTrackDbContext>()
            .UseSqlServer(ConnectionString, sql => sql.UseNetTopologySuite())
            .Options;
}

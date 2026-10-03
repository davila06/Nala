using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PawTrack.Domain.Stores;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.IntegrationTests.Stores;

[Collection("Integration")]
public sealed class StoreOrderIdempotencySqlTests
{
    [Fact]
    public async Task FreshSqlDatabase_AppliesMigrationAndRejectsConcurrentDuplicateKeys()
    {
        if (!OperatingSystem.IsWindows() && Environment.GetEnvironmentVariable("PAWTRACK_SQL_CONNECTION") is null)
            return;

        var baseConnection = Environment.GetEnvironmentVariable("PAWTRACK_SQL_CONNECTION")
            ?? "Server=(localdb)\\MSSQLLocalDB;Integrated Security=True;TrustServerCertificate=True;";
        var connection = new SqlConnectionStringBuilder(baseConnection)
        {
            InitialCatalog = $"PawTrackOrderIdempotency_{Guid.NewGuid():N}",
        };
        var options = new DbContextOptionsBuilder<PawTrackDbContext>()
            .UseSqlServer(connection.ConnectionString, sql => sql.UseNetTopologySuite())
            .Options;

        await using var database = new PawTrackDbContext(options);
        try
        {
            await database.Database.MigrateAsync("20261002201408_AddStoreOrderManualRefundEvidence");
            var legacyOrderId = Guid.NewGuid();
            var legacyStoreId = Guid.NewGuid();
            var legacyCustomerId = Guid.NewGuid();
            await database.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO [StoreOrders]
                    ([Id], [StoreId], [CustomerId], [Status], [FulfillmentType], [PaymentReference], [TotalCrc], [PaymentReportedByCustomer], [PlacedAt])
                VALUES
                    ({legacyOrderId}, {legacyStoreId}, {legacyCustomerId}, 0, 0, {Guid.NewGuid().ToString("N")[..16]}, 1000.00, 0, {DateTimeOffset.UtcNow});
                """);

            await database.Database.MigrateAsync();
            (await database.Database.GetAppliedMigrationsAsync())
                .Should().Contain("20261002212937_AddStoreOrderIdempotencyAndProviderRefundAccounting");
            var upgradedLegacyOrder = await database.StoreOrders.SingleAsync(order => order.Id == legacyOrderId);
            upgradedLegacyOrder.IdempotencyKey.Should().BeNull();
            upgradedLegacyOrder.RequestHash.Should().BeNull();

            var customerId = Guid.NewGuid();
            var idempotencyKey = Guid.NewGuid().ToString("N");
            var firstOrder = CreateOrder(customerId, idempotencyKey, "REF-A");
            var secondOrder = CreateOrder(customerId, idempotencyKey, "REF-B");
            var results = await Task.WhenAll(
                TryInsertAsync(options, firstOrder),
                TryInsertAsync(options, secondOrder));

            results.Count(result => result.Succeeded).Should().Be(1);
            results.Count(result => result.Error is not null).Should().Be(1);
            results.Single(result => result.Error is not null).Error!.ToString()
                .Should().Contain("IX_StoreOrders_CustomerId_IdempotencyKey");
        }
        finally
        {
            await database.Database.EnsureDeletedAsync();
        }
    }

    private static StoreOrder CreateOrder(Guid customerId, string idempotencyKey, string paymentReference) =>
        StoreOrder.Place(
            Guid.NewGuid(), customerId, paymentReference, OrderFulfillmentType.Pickup,
            null, null, [(Guid.NewGuid(), "SQL concurrency item", 1, 1000m)],
            idempotencyKey: idempotencyKey,
            requestHash: new string('A', 64));

    private static async Task<(bool Succeeded, Exception? Error)> TryInsertAsync(
        DbContextOptions<PawTrackDbContext> options,
        StoreOrder order)
    {
        await using var context = new PawTrackDbContext(options);
        context.StoreOrders.Add(order);
        try
        {
            await context.SaveChangesAsync();
            return (true, null);
        }
        catch (DbUpdateException exception)
        {
            return (false, exception);
        }
    }
}
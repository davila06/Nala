using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PawTrack.Domain.Payments;
using PawTrack.Infrastructure.Payments;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.UnitTests.Payments.Infrastructure;

public sealed class PaymentIntentRepositoryTests
{
    [Fact]
    public async Task Finds_intent_by_user_and_idempotency_key()
    {
        var options = new DbContextOptionsBuilder<PawTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new PawTrackDbContext(options);
        var repository = new PaymentIntentRepository(db);
        var userId = Guid.NewGuid();
        var intent = PaymentIntent.Create(userId, 2990m, "CRC", "PT-ORDER-001", "idem-001", "Subscription");

        await repository.AddAsync(intent);
        await db.SaveChangesAsync();

        var found = await repository.GetByIdempotencyKeyAsync(userId, "idem-001");

        found.Should().BeSameAs(intent);
    }

    [Fact]
    public async Task Finds_financial_operation_by_type_and_idempotency_key()
    {
        var options = new DbContextOptionsBuilder<PawTrackDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new PawTrackDbContext(options);
        var repository = new PaymentOperationRepository(db);
        var operation = PaymentOperation.Create(
            Guid.NewGuid(),
            PaymentOperationType.Refund,
            "idem-refund",
            "hash-123",
            "corr-123");

        await repository.AddAsync(operation);
        await db.SaveChangesAsync();

        var found = await repository.GetByIdempotencyKeyAsync(
            PaymentOperationType.Refund,
            "idem-refund");

        found.Should().BeSameAs(operation);
    }
}

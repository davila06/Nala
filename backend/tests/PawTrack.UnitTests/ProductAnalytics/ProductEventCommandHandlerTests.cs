using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ProductAnalytics;

namespace PawTrack.UnitTests.ProductAnalytics;

public sealed class ProductEventCommandHandlerTests
{
    [Fact]
    public async Task Handle_DuplicateEvent_IsIdempotentAndDoesNotSave()
    {
        var repository = Substitute.For<IProductEventRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var webhookFanout = Substitute.For<IWebhookFanout>();
        var eventId = Guid.NewGuid();
        repository.ExistsByEventIdAsync(eventId, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new IngestProductEventCommandHandler(repository, unitOfWork, webhookFanout);

        var result = await handler.Handle(
            new IngestProductEventCommand(eventId, "QrScanned", "1", DateTimeOffset.UtcNow,
                "anonymous", "public-pet-profile", null, Guid.NewGuid(), null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
        await repository.DidNotReceive().AddAsync(Arg.Any<PawTrack.Domain.ProductAnalytics.ProductEvent>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PerformanceQuery_ReturnsCohortConversionsAndSloMetrics()
    {
        var repository = Substitute.For<IProductEventRepository>();
        var from = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        var to = from.AddMonths(1);
        repository.GetPerformanceByCohortAsync(from, to, null, null, null, Arg.Any<CancellationToken>())
            .Returns([
                new ProductCohortMetric(
                    "2026-09", "San José", "Sin especificar", "Sin especificar", 100, 72, 20, 16,
                    48, 240, 60, 300, 80, 75),
            ]);
        repository.GetActiveProtectedCountsAsync(to, Arg.Any<CancellationToken>())
            .Returns(new ActiveProtectedCounts(0, 0, 0));
        var handler = new GetProductPerformanceQueryHandler(repository);

        var result = await handler.Handle(
            new GetProductPerformanceQuery(from, to),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Cohorts.Should().ContainSingle();
        result.Value.Cohorts[0].ActivationRatePercent.Should().Be(72);
        result.Value.Cohorts[0].RecoveryRatePercent.Should().Be(80);
        result.Value.Cohorts[0].FirstResponseSloPercent.Should().Be(75);
    }

    [Fact]
    public async Task PerformanceQuery_ReturnsChannelSpeciesAndActiveProtectedWindows()
    {
        var repository = Substitute.For<IProductEventRepository>();
        var from = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        var to = from.AddMonths(1);
        repository.GetPerformanceByCohortAsync(from, to, "San José", "WhatsApp", "Dog", Arg.Any<CancellationToken>())
            .Returns([
                new ProductCohortMetric(
                    "2026-09", "San José", "WhatsApp", "Dog", 100, 72, 20, 16,
                    48, 240, 60, 300, 80, 75),
            ]);
        repository.GetActiveProtectedCountsAsync(to, Arg.Any<CancellationToken>())
            .Returns(new ActiveProtectedCounts(12, 30, 54));
        var handler = new GetProductPerformanceQueryHandler(repository);

        var result = await handler.Handle(
            new GetProductPerformanceQuery(from, to, "San José", "WhatsApp", "Dog"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Channel.Should().Be("WhatsApp");
        result.Value.Species.Should().Be("Dog");
        result.Value.ActiveProtectedPets30Days.Should().Be(12);
        result.Value.ActiveProtectedPets90Days.Should().Be(30);
        result.Value.ActiveProtectedPets180Days.Should().Be(54);
        result.Value.Cohorts[0].Channel.Should().Be("WhatsApp");
        result.Value.Cohorts[0].Species.Should().Be("Dog");
    }
}

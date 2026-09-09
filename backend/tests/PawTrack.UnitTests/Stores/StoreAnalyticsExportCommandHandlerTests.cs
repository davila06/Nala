using FluentAssertions;
using MediatR;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Stores;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;

namespace PawTrack.UnitTests.Stores;

public sealed class StoreAnalyticsExportCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenMonthlyQuotaIsExhausted_ReturnsFailureWithoutGeneratingExport()
    {
        var sender = Substitute.For<ISender>();
        var audit = Substitute.For<IAuditLogRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var ownerId = Guid.NewGuid();

        audit.CountByActionSinceAsync(AuditAction.StoreAnalyticsExported, ownerId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(20);

        var handler = new ExportStoreAnalyticsCommandHandler(sender, audit, unitOfWork);
        var result = await handler.Handle(new ExportStoreAnalyticsCommand(ownerId, 2026, 9), default);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Contains("límite mensual", StringComparison.OrdinalIgnoreCase));
        await sender.DidNotReceiveWithAnyArgs().Send(default!, default);
        await audit.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }
}
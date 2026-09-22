using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Audit;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;

namespace PawTrack.UnitTests.Audit;

public sealed class GetAuditLogQueryHandlerTests
{
    [Fact]
    public async Task Filters_audit_entries_by_actor_and_period()
    {
        var repository = Substitute.For<IAuditLogRepository>();
        var actorId = Guid.NewGuid();
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var entry = AuditLogEntry.Create(actorId, AuditAction.SubscriptionRenewed, "Subscription", Guid.NewGuid().ToString());
        repository.GetFilteredAsync(
                Arg.Is<AuditLogFilter>(filter =>
                    filter.ActorId == actorId && filter.From == from && filter.To == to && filter.Take == 25),
                Arg.Any<CancellationToken>())
            .Returns(new[] { entry });

        var result = await new GetAuditLogQueryHandler(repository).Handle(
            new GetAuditLogQuery(null, null, 25, actorId, from, to), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.AdminUserId.Should().Be(actorId.ToString());
    }
}

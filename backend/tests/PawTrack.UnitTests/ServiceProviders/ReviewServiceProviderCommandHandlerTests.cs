using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class ReviewServiceProviderCommandHandlerTests
{
    [Fact]
    public async Task Handle_Approve_ActivatesProviderAndRecordsAuditEntry()
    {
        var providers = Substitute.For<IServiceProviderRepository>();
        var auditLog = Substitute.For<IAuditLogRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var provider = ServiceProvider.Create(
            Guid.NewGuid(), "Grooming CR", "Cuidado profesional", ServiceProviderCategory.Groomer,
            "Heredia", 10m, -84m, "grooming@example.cr");
        var adminUserId = Guid.NewGuid();
        providers.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>()).Returns(provider);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new ReviewServiceProviderCommandHandler(providers, auditLog, unitOfWork);
        var result = await handler.Handle(
            new ReviewServiceProviderCommand(adminUserId, provider.Id, Approve: true), default);

        result.IsSuccess.Should().BeTrue();
        provider.Status.Should().Be(ServiceProviderStatus.Active);
        await auditLog.Received(1).AddAsync(
            Arg.Is<AuditLogEntry>(entry =>
                entry.AdminUserId == adminUserId && entry.Action == AuditAction.ServiceProviderApproved),
            Arg.Any<CancellationToken>());
    }
}
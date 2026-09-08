using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class SetServiceProviderMembershipCommandHandlerTests
{
    [Fact]
    public async Task Handle_GrantsManualFeaturedMembership_ExemptFromTrialExpiration()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var auditLog = Substitute.For<IAuditLogRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var adminUserId = Guid.NewGuid();
        var provider = ServiceProvider.Create(
            Guid.NewGuid(), "Grooming CR", "Cuidado profesional", ServiceProviderCategory.Groomer,
            "Heredia", 10m, -84m, "grooming@example.cr");
        repository.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>()).Returns(provider);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new SetServiceProviderMembershipCommandHandler(repository, auditLog, unitOfWork);
        var result = await handler.Handle(new SetServiceProviderMembershipCommand(
            adminUserId, provider.Id, ProviderMembershipTier.Featured, Manual: true), default);

        result.IsSuccess.Should().BeTrue();
        provider.MembershipTier.Should().Be(ProviderMembershipTier.Featured);
        provider.IsMembershipManual.Should().BeTrue();
        provider.ExpireTrialIfDue(DateTimeOffset.UtcNow.AddYears(1)).Should().BeFalse();
        await auditLog.Received(1).AddAsync(
            Arg.Is<AuditLogEntry>(entry =>
                entry.AdminUserId == adminUserId && entry.Action == AuditAction.ServiceProviderMembershipChanged),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProviderNotFound_ReturnsFailure()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var auditLog = Substitute.For<IAuditLogRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new SetServiceProviderMembershipCommandHandler(repository, auditLog, unitOfWork);
        var result = await handler.Handle(new SetServiceProviderMembershipCommand(
            Guid.NewGuid(), Guid.NewGuid(), ProviderMembershipTier.Verified, Manual: false), default);

        result.IsFailure.Should().BeTrue();
    }
}

using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class ReviewProviderVerificationCommandHandlerTests
{
    [Fact]
    public async Task Handle_ApprovedVerification_RecordsAdminAudit()
    {
        var providers = Substitute.For<IServiceProviderRepository>();
        var audit = Substitute.For<IAuditLogRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var verification = ProviderVerification.Submit(Guid.NewGuid(), Guid.NewGuid());
        verification.AttachDocument("https://storage.example/provider-verification/document.pdf");
        var adminUserId = Guid.NewGuid();
        providers.GetVerificationByIdAsync(verification.Id, Arg.Any<CancellationToken>()).Returns(verification);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new ReviewProviderVerificationCommandHandler(providers, audit, unitOfWork);
        var result = await handler.Handle(new ReviewProviderVerificationCommand(
            verification.Id, adminUserId, true, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)), null), default);

        result.IsSuccess.Should().BeTrue();
        verification.Status.Should().Be(ProviderVerificationStatus.Verified);
        await audit.Received(1).AddAsync(
            Arg.Is<AuditLogEntry>(entry => entry.Action == AuditAction.ProviderVerificationApproved && entry.AdminUserId == adminUserId),
            Arg.Any<CancellationToken>());
    }
}
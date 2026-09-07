using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class SetServiceProviderOperationalStatusCommandHandlerTests
{
    [Fact]
    public async Task Handle_SuspendWithReason_SuspendsAndAuditsProvider()
    {
        var providers = Substitute.For<IServiceProviderRepository>();
        var audit = Substitute.For<IAuditLogRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var provider = ServiceProvider.Create(Guid.NewGuid(), "Grooming", "Cuidado", ServiceProviderCategory.Groomer, "Heredia", 10m, -84m, "provider@example.cr");
        provider.Activate();
        var adminUserId = Guid.NewGuid();
        providers.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>()).Returns(provider);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new SetServiceProviderOperationalStatusCommandHandler(providers, audit, unitOfWork);
        var result = await handler.Handle(new SetServiceProviderOperationalStatusCommand(adminUserId, provider.Id, Suspend: true, "Incidente abierto"), default);

        result.IsSuccess.Should().BeTrue();
        provider.Status.Should().Be(ServiceProviderStatus.Suspended);
        provider.SuspensionReason.Should().Be("Incidente abierto");
        await audit.Received(1).AddAsync(
            Arg.Is<AuditLogEntry>(entry => entry.Action == AuditAction.ServiceProviderSuspended && entry.Details == "Incidente abierto"),
            Arg.Any<CancellationToken>());
    }
}
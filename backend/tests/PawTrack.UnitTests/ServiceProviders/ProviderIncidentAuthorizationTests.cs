using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class ProviderIncidentAuthorizationTests
{
    [Fact]
    public async Task OpenIncident_ProviderOwnerCannotOpenIncidentForAnotherProvider()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var auditLog = Substitute.For<PawTrack.Application.Common.Interfaces.IAuditLogRepository>();
        var unitOfWork = Substitute.For<PawTrack.Application.Common.Interfaces.IUnitOfWork>();
        var reporterId = Guid.NewGuid();
        var targetOwnerId = Guid.NewGuid();
        var targetProvider = ServiceProvider.Create(
            targetOwnerId, "Target", "Cuidado", ServiceProviderCategory.Other,
            "San Jose", 9.9m, -84m, "target@example.cr");
        repository.GetByIdAsync(targetProvider.Id, Arg.Any<CancellationToken>()).Returns(targetProvider);

        var handler = new OpenProviderIncidentCommandHandler(repository, auditLog, unitOfWork);
        var result = await handler.Handle(new OpenProviderIncidentCommand(
            reporterId, targetProvider.Id, ProviderIncidentType.Policy, "Incidente", null), default);

        result.IsFailure.Should().BeTrue();
        await repository.DidNotReceive().AddIncidentAsync(Arg.Any<ProviderIncident>(), Arg.Any<CancellationToken>());
    }
}
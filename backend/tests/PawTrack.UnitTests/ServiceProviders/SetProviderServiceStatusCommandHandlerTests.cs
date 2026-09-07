using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class SetProviderServiceStatusCommandHandlerTests
{
    [Fact]
    public async Task Handle_OwnersPublishedService_PausesIt()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var ownerUserId = Guid.NewGuid();
        var provider = ServiceProvider.Create(ownerUserId, "Grooming", "Cuidado", ServiceProviderCategory.Groomer, "Heredia", 10m, -84m, "owner@example.cr");
        var service = ProviderService.Create(provider.Id, "Bano", "Bano", ServiceModality.AtProviderLocation, 60, 20_000m, 1);
        repository.GetByUserIdAsync(ownerUserId, Arg.Any<CancellationToken>()).Returns(provider);
        repository.GetServiceByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new SetProviderServiceStatusCommandHandler(repository, unitOfWork);
        var result = await handler.Handle(new SetProviderServiceStatusCommand(ownerUserId, service.Id, ProviderServiceStatus.Paused), default);

        result.IsSuccess.Should().BeTrue();
        service.Status.Should().Be(ProviderServiceStatus.Paused);
    }

    [Fact]
    public async Task Handle_Pause_RecordsAuditEntry()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var auditLog = Substitute.For<IAuditLogRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var ownerUserId = Guid.NewGuid();
        var provider = ServiceProvider.Create(ownerUserId, "Grooming", "Cuidado", ServiceProviderCategory.Groomer, "Heredia", 10m, -84m, "owner@example.cr");
        var service = ProviderService.Create(provider.Id, "Bano", "Bano", ServiceModality.AtProviderLocation, 60, 20_000m, 1);
        repository.GetByUserIdAsync(ownerUserId, Arg.Any<CancellationToken>()).Returns(provider);
        repository.GetServiceByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new SetProviderServiceStatusCommandHandler(repository, unitOfWork, auditLog);
        await handler.Handle(new SetProviderServiceStatusCommand(ownerUserId, service.Id, ProviderServiceStatus.Paused), default);

        await auditLog.Received(1).AddAsync(
            Arg.Is<AuditLogEntry>(entry => entry.Action == AuditAction.ProviderServicePaused && entry.AdminUserId == ownerUserId),
            Arg.Any<CancellationToken>());
    }
}
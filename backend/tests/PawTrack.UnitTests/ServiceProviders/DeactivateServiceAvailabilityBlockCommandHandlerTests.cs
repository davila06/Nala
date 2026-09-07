using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.Common;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class DeactivateServiceAvailabilityBlockCommandHandlerTests
{
    [Fact]
    public async Task Handle_OwnersBlock_DeactivatesWithoutDeletingHistory()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var ownerUserId = Guid.NewGuid();
        var provider = ServiceProvider.Create(ownerUserId, "Grooming", "Cuidado", ServiceProviderCategory.Groomer, "Heredia", 10m, -84m, "owner@example.cr");
        var service = ProviderService.Create(provider.Id, "Bano", "Bano", ServiceModality.AtProviderLocation, 60, 20_000m, 1);
        var block = ServiceAvailabilityBlock.Create(service.Id, DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(1).AddHours(2), "Cierre");
        repository.GetByUserIdAsync(ownerUserId, Arg.Any<CancellationToken>()).Returns(provider);
        repository.GetServiceByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        repository.GetAvailabilityBlockByIdAsync(block.Id, Arg.Any<CancellationToken>()).Returns(block);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new DeactivateServiceAvailabilityBlockCommandHandler(repository, unitOfWork);
        var result = await handler.Handle(new DeactivateServiceAvailabilityBlockCommand(ownerUserId, block.Id), default);

        result.IsSuccess.Should().BeTrue();
        block.IsActive.Should().BeFalse();
    }
}
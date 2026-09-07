using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.Common;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class UpdateProviderServiceCommandHandlerTests
{
    [Fact]
    public async Task Handle_OwnersService_UpdatesBookableDetails()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var ownerUserId = Guid.NewGuid();
        var provider = ServiceProvider.Create(ownerUserId, "Grooming", "Cuidado", ServiceProviderCategory.Groomer, "Heredia", 10m, -84m, "owner@example.cr");
        var service = ProviderService.Create(provider.Id, "Bano", "Basico", ServiceModality.AtProviderLocation, 60, 20_000m, 1);
        repository.GetByUserIdAsync(ownerUserId, Arg.Any<CancellationToken>()).Returns(provider);
        repository.GetServiceByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        var handler = new UpdateProviderServiceCommandHandler(repository, unitOfWork);
        var result = await handler.Handle(new UpdateProviderServiceCommand(
            ownerUserId, service.Id, "Bano completo", "Incluye secado", ServiceModality.AtProviderLocation, 90, 30_000m, 2), default);

        result.IsSuccess.Should().BeTrue();
        service.Name.Should().Be("Bano completo");
        service.DurationMinutes.Should().Be(90);
        service.PriceCrc.Should().Be(30_000m);
        service.Capacity.Should().Be(2);
    }
}
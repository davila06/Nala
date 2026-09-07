using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class GetServiceAvailabilityBlocksQueryHandlerTests
{
    [Fact]
    public async Task Handle_OwnersService_ReturnsUpcomingBlocks()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var ownerUserId = Guid.NewGuid();
        var provider = ServiceProvider.Create(ownerUserId, "Grooming", "Cuidado", ServiceProviderCategory.Groomer, "Heredia", 10m, -84m, "owner@example.cr");
        var service = ProviderService.Create(provider.Id, "Bano", "Bano", ServiceModality.AtProviderLocation, 60, 20_000m, 1);
        var now = DateTimeOffset.UtcNow;
        var block = ServiceAvailabilityBlock.Create(service.Id, now.AddDays(2), now.AddDays(2).AddHours(2), "Cierre");
        repository.GetByUserIdAsync(ownerUserId, Arg.Any<CancellationToken>()).Returns(provider);
        repository.GetServiceByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        repository.GetAvailabilityBlocksByServiceRangeAsync(service.Id, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns([block]);

        var handler = new GetServiceAvailabilityBlocksQueryHandler(repository);
        var result = await handler.Handle(new GetServiceAvailabilityBlocksQuery(ownerUserId, service.Id, now, now.AddDays(90)), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.Reason.Should().Be("Cierre");
    }
}
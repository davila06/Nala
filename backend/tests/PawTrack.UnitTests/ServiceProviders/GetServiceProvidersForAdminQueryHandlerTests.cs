using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class GetServiceProvidersForAdminQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsOperationalProviderProfiles()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var provider = ServiceProvider.Create(Guid.NewGuid(), "Grooming", "Cuidado", ServiceProviderCategory.Groomer, "Heredia", 10m, -84m, "owner@example.cr");
        provider.Activate();
        repository.GetOperationalProvidersAsync(0, 50, Arg.Any<CancellationToken>()).Returns([provider]);

        var handler = new GetServiceProvidersForAdminQueryHandler(repository);
        var result = await handler.Handle(new GetServiceProvidersForAdminQuery(1, 50), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.Status.Should().Be("Active");
    }
}
using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class GetPublicServiceProvidersQueryHandlerTests
{
    [Fact]
    public async Task Handle_PassesModalityAndPriceRangeToRepository()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        repository.GetActivePagedAsync(
            ServiceProviderCategory.Groomer,
            ServiceModality.AtCustomerLocation,
            10_000m,
            30_000m,
            0,
            20,
            Arg.Any<CancellationToken>()).Returns([]);
        var handler = new GetPublicServiceProvidersQueryHandler(repository);

        var result = await handler.Handle(new GetPublicServiceProvidersQuery(
            ServiceProviderCategory.Groomer,
            ServiceModality.AtCustomerLocation,
            10_000m,
            30_000m,
            1,
            20), default);

        result.IsSuccess.Should().BeTrue();
        await repository.Received(1).GetActivePagedAsync(
            ServiceProviderCategory.Groomer,
            ServiceModality.AtCustomerLocation,
            10_000m,
            30_000m,
            0,
            20,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ActiveVerification_AddsPublicVerifiedIndicator()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var provider = ServiceProvider.Create(Guid.NewGuid(), "Grooming", "Cuidado", ServiceProviderCategory.Groomer, "Heredia", 10m, -84m, "owner@example.cr");
        provider.Activate();
        repository.GetActivePagedAsync(null, null, null, null, 0, 20, Arg.Any<CancellationToken>()).Returns([provider]);
        repository.GetActiveVerifiedProviderIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid> { provider.Id }));
        var handler = new GetPublicServiceProvidersQueryHandler(repository);

        var result = await handler.Handle(new GetPublicServiceProvidersQuery(Page: 1, PageSize: 20), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.IsVerified.Should().BeTrue();
    }
}
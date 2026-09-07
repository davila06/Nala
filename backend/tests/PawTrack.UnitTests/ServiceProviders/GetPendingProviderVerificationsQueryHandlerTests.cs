using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class GetPendingProviderVerificationsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsOnlyPendingVerificationDtos()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var pending = ProviderVerification.Submit(Guid.NewGuid(), Guid.NewGuid());
        repository.GetPendingVerificationsAsync(0, 20, Arg.Any<CancellationToken>()).Returns([pending]);

        var handler = new GetPendingProviderVerificationsQueryHandler(repository);
        var result = await handler.Handle(new GetPendingProviderVerificationsQuery(1, 20), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.Id.Should().Be(pending.Id);
    }
}
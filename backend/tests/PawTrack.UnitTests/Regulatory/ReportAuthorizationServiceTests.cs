using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Domain.Municipalities;
using PawTrack.Domain.Regulatory;
using PawTrack.Infrastructure.Regulatory;

namespace PawTrack.UnitTests.Regulatory;

public sealed class ReportAuthorizationServiceTests
{
    [Fact]
    public async Task MunicipalUser_CannotRequestAnotherCanton()
    {
        var municipalities = Substitute.For<IMunicipalSubscriptionService>();
        municipalities.GetAuthorizedCantonsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(["San José"]);
        var service = new ReportAuthorizationService(municipalities);

        var result = await service.AuthorizeAsync(
            Guid.NewGuid(),
            ExportScope.Institutional,
            "Heredia",
            null,
            ReportType.MunicipalCaptures);

        result.IsAuthorized.Should().BeFalse();
    }

    [Fact]
    public async Task PublicScope_DoesNotRequireCanton()
    {
        var municipalities = Substitute.For<IMunicipalSubscriptionService>();
        var service = new ReportAuthorizationService(municipalities);

        var result = await service.AuthorizeAsync(
            Guid.NewGuid(),
            ExportScope.Public,
            null,
            null,
            ReportType.Recovery);

        result.IsAuthorized.Should().BeTrue();
    }
}

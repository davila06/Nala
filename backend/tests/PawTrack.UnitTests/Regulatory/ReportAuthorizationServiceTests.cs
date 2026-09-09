using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Municipalities.Interfaces;
using PawTrack.Application.Common.Interfaces;
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
        var profiles = Substitute.For<IMunicipalProfileRepository>();
        var users = Substitute.For<IUserRepository>();
        municipalities.GetAuthorizedCantonsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(["San José"]);
        var service = new ReportAuthorizationService(municipalities, profiles, users);

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
        var service = new ReportAuthorizationService(
            municipalities,
            Substitute.For<IMunicipalProfileRepository>(),
            Substitute.For<IUserRepository>());

        var result = await service.AuthorizeAsync(
            Guid.NewGuid(),
            ExportScope.Public,
            null,
            null,
            ReportType.Recovery);

        result.IsAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task User_CannotRequestAnotherOrganization()
    {
        var userId = Guid.NewGuid();
        var ownProfile = MunicipalityProfile.Create(userId, "San José", "Municipalidad", MunicipalTier.Full);
        var municipalities = Substitute.For<IMunicipalSubscriptionService>();
        var profiles = Substitute.For<IMunicipalProfileRepository>();
        var users = Substitute.For<IUserRepository>();
        profiles.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(ownProfile);
        users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((PawTrack.Domain.Auth.User?)null);
        var service = new ReportAuthorizationService(municipalities, profiles, users);

        var result = await service.AuthorizeAsync(userId, ExportScope.Institutional,
            "San José", Guid.NewGuid(), ReportType.MunicipalCaptures);

        result.IsAuthorized.Should().BeFalse();
    }

    [Fact]
    public async Task InstitutionalScope_WithoutMunicipalProfile_IsDenied()
    {
        var userId = Guid.NewGuid();
        var municipalities = Substitute.For<IMunicipalSubscriptionService>();
        var profiles = Substitute.For<IMunicipalProfileRepository>();
        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((PawTrack.Domain.Auth.User?)null);
        profiles.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns((MunicipalityProfile?)null);
        var service = new ReportAuthorizationService(municipalities, profiles, users);

        var result = await service.AuthorizeAsync(
            userId,
            ExportScope.Institutional,
            "San José",
            null,
            ReportType.MunicipalCaptures);

        result.IsAuthorized.Should().BeFalse();
    }

    [Fact]
    public async Task InstitutionalScope_NonAdminWithoutCanton_IsDenied()
    {
        var userId = Guid.NewGuid();
        var profile = MunicipalityProfile.Create(userId, "San José", "Municipalidad", MunicipalTier.Full);
        var municipalities = Substitute.For<IMunicipalSubscriptionService>();
        var profiles = Substitute.For<IMunicipalProfileRepository>();
        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((PawTrack.Domain.Auth.User?)null);
        profiles.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);
        var service = new ReportAuthorizationService(municipalities, profiles, users);

        var result = await service.AuthorizeAsync(
            userId,
            ExportScope.Institutional,
            null,
            profile.Id,
            ReportType.MunicipalCaptures);

        result.IsAuthorized.Should().BeFalse();
    }

    [Fact]
    public async Task InstitutionalScope_EmptyCantonSubscription_IsDenied()
    {
        var userId = Guid.NewGuid();
        var profile = MunicipalityProfile.Create(userId, "San José", "Municipalidad", MunicipalTier.Full);
        var municipalities = Substitute.For<IMunicipalSubscriptionService>();
        var profiles = Substitute.For<IMunicipalProfileRepository>();
        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((PawTrack.Domain.Auth.User?)null);
        profiles.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);
        municipalities.GetAuthorizedCantonsAsync(userId, Arg.Any<CancellationToken>()).Returns([]);
        var service = new ReportAuthorizationService(municipalities, profiles, users);

        var result = await service.AuthorizeAsync(
            userId,
            ExportScope.Institutional,
            "San José",
            profile.Id,
            ReportType.MunicipalCaptures);

        result.IsAuthorized.Should().BeFalse();
    }

    [Theory]
    [InlineData(ExportScope.Admin)]
    [InlineData(ExportScope.Nala)]
    public async Task PrivilegedScope_NonAdminUser_IsDenied(ExportScope scope)
    {
        var userId = Guid.NewGuid();
        var municipalities = Substitute.For<IMunicipalSubscriptionService>();
        var profiles = Substitute.For<IMunicipalProfileRepository>();
        var users = Substitute.For<IUserRepository>();
        users.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((PawTrack.Domain.Auth.User?)null);
        var service = new ReportAuthorizationService(municipalities, profiles, users);

        var result = await service.AuthorizeAsync(
            userId, scope, null, null, ReportType.NalaOverview);

        result.IsAuthorized.Should().BeFalse();
    }
}

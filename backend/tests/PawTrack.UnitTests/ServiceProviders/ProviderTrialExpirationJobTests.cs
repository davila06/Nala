using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.Common;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class ProviderTrialExpirationJobTests
{
    [Fact]
    public async Task ExecuteAsync_ExpiredTrial_DowngradesToFree()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var provider = ServiceProvider.Create(
            Guid.NewGuid(), "Grooming CR", "Cuidado profesional", ServiceProviderCategory.Groomer,
            "Heredia", 10m, -84m, "grooming@example.cr");
        provider.Activate();
        typeof(ServiceProvider).GetProperty("TrialEndsAt")!.SetValue(provider, DateTimeOffset.UtcNow.AddDays(-1));
        repository.GetProvidersWithExpiredTrialAsync(Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([provider]);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        await new ProviderTrialExpirationJob(repository, unitOfWork).ExecuteAsync(default);

        provider.MembershipTier.Should().Be(ProviderMembershipTier.Free);
        repository.Received(1).Update(provider);
    }

    [Fact]
    public async Task ExecuteAsync_NoExpiredTrials_DoesNotSave()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        repository.GetProvidersWithExpiredTrialAsync(Arg.Any<DateTimeOffset>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await new ProviderTrialExpirationJob(repository, unitOfWork).ExecuteAsync(default);

        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

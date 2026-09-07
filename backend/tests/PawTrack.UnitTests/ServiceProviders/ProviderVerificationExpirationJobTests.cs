using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ServiceProviders;
using PawTrack.Domain.Common;
using PawTrack.Domain.ServiceProviders;

namespace PawTrack.UnitTests.ServiceProviders;

public sealed class ProviderVerificationExpirationJobTests
{
    [Fact]
    public async Task ExecuteAsync_ExpiredVerification_MarksItExpired()
    {
        var repository = Substitute.For<IServiceProviderRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var verification = ProviderVerification.Submit(Guid.NewGuid(), Guid.NewGuid());
        verification.AttachDocument("https://storage.example/document.pdf");
        verification.Verify(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), null);
        repository.GetVerifiedVerificationsExpiredBeforeAsync(Arg.Any<DateOnly>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([verification]);
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        await new ProviderVerificationExpirationJob(repository, unitOfWork).ExecuteAsync(default);

        verification.Status.Should().Be(ProviderVerificationStatus.Expired);
    }
}
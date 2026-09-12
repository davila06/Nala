using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Interfaces;
using PawTrack.Application.Subscriptions.Queries.GetSubscriptionQrCode;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.UnitTests.Subscriptions.Queries;

public sealed class GetSubscriptionQrCodeQueryHandlerTests
{
    private readonly ISubscriptionRepository _subscriptionRepo = Substitute.For<ISubscriptionRepository>();
    private readonly IQrCodeService _qrCodeService = Substitute.For<IQrCodeService>();
    private readonly IConfiguration _configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["App:SinpePhone"] = "8888-9999",
        })
        .Build();

    private GetSubscriptionQrCodeQueryHandler CreateSut() =>
        new(_subscriptionRepo, _qrCodeService, _configuration);

    [Fact]
    public async Task Handle_WhenSubscriptionNotFound_ReturnsFailure()
    {
        _subscriptionRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Subscription?)null);

        var sut = CreateSut();
        var result = await sut.Handle(new GetSubscriptionQrCodeQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Subscription not found.");
    }

    [Fact]
    public async Task Handle_WhenValidOwner_GeneratesQrCodeWithSinpePaseFormat()
    {
        var ownerId = Guid.NewGuid();
        var sub = Subscription.CreateForUser(ownerId, SubscriptionTier.UserPlus, "REF99887", 2990m);
        var expectedBytes = new byte[] { 1, 2, 3, 4, 5 };

        _subscriptionRepo.GetByIdAsync(sub.Id, Arg.Any<CancellationToken>())
            .Returns(sub);
        _qrCodeService.GeneratePng(Arg.Is<string>(s => s.Contains("PASE 2990 88889999 REF99887")))
            .Returns(expectedBytes);

        var sut = CreateSut();
        var result = await sut.Handle(new GetSubscriptionQrCodeQuery(sub.Id, ownerId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedBytes);
    }
}

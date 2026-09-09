using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.ProductAnalytics;

namespace PawTrack.UnitTests.ProductAnalytics;

public sealed class ProductEventCommandHandlerTests
{
    [Fact]
    public async Task Handle_DuplicateEvent_IsIdempotentAndDoesNotSave()
    {
        var repository = Substitute.For<IProductEventRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var webhookFanout = Substitute.For<IWebhookFanout>();
        var eventId = Guid.NewGuid();
        repository.ExistsByEventIdAsync(eventId, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new IngestProductEventCommandHandler(repository, unitOfWork, webhookFanout);

        var result = await handler.Handle(
            new IngestProductEventCommand(eventId, "QrScanned", "1", DateTimeOffset.UtcNow,
                "anonymous", "public-pet-profile", null, Guid.NewGuid(), null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
        await repository.DidNotReceive().AddAsync(Arg.Any<PawTrack.Domain.ProductAnalytics.ProductEvent>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
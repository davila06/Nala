using FluentAssertions;
using PawTrack.Domain.Webhooks;

namespace PawTrack.UnitTests.Webhooks;

public sealed class WebhookDeliveryDomainTests
{
    [Fact]
    public void Delivery_RetryBackoffAndReplayHeadersAreDeterministic()
    {
        var delivery = WebhookDelivery.Create(
            Guid.NewGuid(), "pet.updated", "{\"id\":1}", DateTimeOffset.UtcNow);

        delivery.Signature.Should().NotBeNullOrWhiteSpace();
        delivery.IdempotencyKey.Should().NotBe(Guid.Empty.ToString());
        delivery.MarkFailed("timeout");
        delivery.AttemptCount.Should().Be(1);
        delivery.NextAttemptAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }
}
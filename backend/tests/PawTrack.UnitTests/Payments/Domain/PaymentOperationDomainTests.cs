using FluentAssertions;
using PawTrack.Domain.Payments;

namespace PawTrack.UnitTests.Payments.Domain;

public sealed class PaymentOperationDomainTests
{
    [Fact]
    public void Create_starts_processing_with_a_normalized_request_identity()
    {
        var operation = PaymentOperation.Create(
            Guid.NewGuid(),
            PaymentOperationType.Capture,
            " idem-capture ",
            "hash-123",
            "corr-123");

        operation.Status.Should().Be(PaymentOperationStatus.Processing);
        operation.IdempotencyKey.Should().Be("idem-capture");
        operation.RequestHash.Should().Be("hash-123");
        operation.CorrelationId.Should().Be("corr-123");
        operation.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Succeeded_operation_cannot_be_completed_twice()
    {
        var operation = PaymentOperation.Create(
            Guid.NewGuid(),
            PaymentOperationType.Refund,
            "idem-refund",
            "hash-123",
            "corr-123");

        operation.MarkSucceeded("provider-refund-1", "{\"status\":\"SUCCEEDED\"}");

        operation.Status.Should().Be(PaymentOperationStatus.Succeeded);
        operation.ProviderOperationId.Should().Be("provider-refund-1");
        operation.ResponseJson.Should().Contain("SUCCEEDED");
        operation.MarkSucceeded("provider-refund-2", "{}");
        operation.ProviderOperationId.Should().Be("provider-refund-1");
    }

    [Fact]
    public void Unknown_operation_preserves_provider_uncertainty()
    {
        var operation = PaymentOperation.Create(
            null,
            PaymentOperationType.WebhookReceived,
            "provider-event-1",
            "hash-123",
            "corr-123");

        operation.MarkUnknown("provider timeout", "{\"status\":\"UNKNOWN\"}");

        operation.Status.Should().Be(PaymentOperationStatus.Unknown);
        operation.FailureReason.Should().Be("provider timeout");
    }
}

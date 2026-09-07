using FluentAssertions;
using PawTrack.Application.Regulatory.Gateways;
using PawTrack.Application.Regulatory.Interfaces;
using PawTrack.Domain.Regulatory;

namespace PawTrack.UnitTests.Regulatory;

public sealed class RegulatorySubmissionGatewayTests
{
    [Fact]
    public async Task NoOpGateway_DoesNotSendExternally_AndReturnsPreparedResult()
    {
        var gateway = new NoOpRegulatorySubmissionGateway();
        var package = new RegulatorySubmissionPackage(
            Guid.NewGuid(),
            "WELFARE_CASES",
            "1.0",
            ExportFormat.Json,
            new string('a', 64),
            [1, 2, 3]);

        var result = await gateway.SubmitAsync(package);

        result.Status.Should().Be(RegulatorySubmissionStatus.Prepared);
        result.ExternalReference.Should().BeNull();
        result.Message.Should().Contain("NoOp");
    }
}

using PawTrack.Application.Regulatory.Interfaces;
using PawTrack.Domain.Regulatory;

namespace PawTrack.Application.Regulatory.Gateways;

public sealed class NoOpRegulatorySubmissionGateway : IRegulatorySubmissionGateway
{
    public Task<RegulatorySubmissionResult> SubmitAsync(
        RegulatorySubmissionPackage package,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new RegulatorySubmissionResult(
            RegulatorySubmissionStatus.Prepared,
            ExternalReference: null,
            "NoOp: no se realizó envío externo."));
}

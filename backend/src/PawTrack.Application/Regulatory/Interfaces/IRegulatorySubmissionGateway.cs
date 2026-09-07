using PawTrack.Domain.Regulatory;

namespace PawTrack.Application.Regulatory.Interfaces;

public sealed record RegulatorySubmissionPackage(
    Guid ExportId,
    string ReportType,
    string SchemaVersion,
    ExportFormat Format,
    string PayloadSha256,
    byte[] Payload);

public sealed record RegulatorySubmissionResult(
    RegulatorySubmissionStatus Status,
    string? ExternalReference,
    string Message);

public interface IRegulatorySubmissionGateway
{
    Task<RegulatorySubmissionResult> SubmitAsync(
        RegulatorySubmissionPackage package,
        CancellationToken cancellationToken = default);
}

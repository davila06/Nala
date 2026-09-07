using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Regulatory.Dtos;
using PawTrack.Application.Regulatory.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;
using PawTrack.Domain.Regulatory;
using Microsoft.ApplicationInsights;

namespace PawTrack.Application.Regulatory.Commands;

public sealed record PrepareRegulatorySubmissionCommand(
    Guid ExportId,
    Guid RequestedByUserId,
    string Destination,
    string SubmissionType,
    string IdempotencyKey) : IRequest<Result<RegulatorySubmissionDto>>;

public sealed record RegulatorySubmissionDto(
    Guid Id,
    Guid ExportId,
    string Destination,
    string SubmissionType,
    RegulatorySubmissionStatus Status,
    string PayloadSha256,
    DateTimeOffset CreatedAt);

public sealed class PrepareRegulatorySubmissionCommandHandler(
    IRegulatoryExportRepository exportRepository,
    IReportAuthorizationService authorizationService,
    IRegulatorySubmissionRepository submissionRepository,
    IRegulatorySubmissionGateway gateway,
    IBlobStorageService blobStorage,
    IAuditLogRepository auditLogRepository,
    TelemetryClient telemetryClient,
    IUnitOfWork unitOfWork)
    : IRequestHandler<PrepareRegulatorySubmissionCommand, Result<RegulatorySubmissionDto>>
{
    public async Task<Result<RegulatorySubmissionDto>> Handle(PrepareRegulatorySubmissionCommand request, CancellationToken ct)
    {
        var export = await exportRepository.GetByIdAsync(request.ExportId, ct);
        if (export is null || !export.IsDownloadable)
            return Result.Failure<RegulatorySubmissionDto>("Export no encontrado o no disponible.");
        var authorization = await authorizationService.AuthorizeAsync(
            request.RequestedByUserId, export.Scope, export.Canton, export.OrganizationId, export.ReportType, ct);
        if (!authorization.IsAuthorized)
            return Result.Failure<RegulatorySubmissionDto>("Export no encontrado o no disponible.");

        var existing = await submissionRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, ct);
        if (existing is not null) return Result.Success(ToDto(existing));

        var submission = RegulatorySubmission.Prepare(
            export.Id,
            request.Destination,
            request.SubmissionType,
            request.IdempotencyKey,
            export.PayloadSha256!,
            export.BlobUrl);
        var payload = await blobStorage.DownloadAsync(export.BlobUrl!, ct);
        if (payload is null)
            return Result.Failure<RegulatorySubmissionDto>("No fue posible leer el payload privado del export.");
        await submissionRepository.AddAsync(submission, ct);
        await auditLogRepository.AddAsync(AuditLogEntry.Create(
            request.RequestedByUserId,
            AuditAction.RegulatorySubmissionPrepared,
            "RegulatorySubmission",
            submission.Id.ToString(),
            "Prepared; no external submission activated."), ct);
        await unitOfWork.SaveChangesAsync(ct);

        // The gateway is intentionally NoOp until an approved institutional channel exists.
        _ = await gateway.SubmitAsync(new RegulatorySubmissionPackage(
            export.Id,
            export.ReportType.ToString(),
            export.SchemaVersion,
            export.Format,
            export.PayloadSha256!,
            payload), ct);
        telemetryClient.TrackEvent("RegulatorySubmission.Prepared", new Dictionary<string, string>
        {
            ["submissionType"] = request.SubmissionType,
            ["destination"] = request.Destination,
        });

        return Result.Success(ToDto(submission));
    }

    private static RegulatorySubmissionDto ToDto(RegulatorySubmission submission) => new(
        submission.Id,
        submission.ExportId,
        submission.Destination,
        submission.SubmissionType,
        submission.Status,
        submission.PayloadSha256,
        submission.CreatedAt);
}

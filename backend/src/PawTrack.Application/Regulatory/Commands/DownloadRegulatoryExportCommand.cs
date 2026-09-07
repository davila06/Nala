using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Regulatory.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;
using PawTrack.Domain.Regulatory;
using Microsoft.ApplicationInsights;

namespace PawTrack.Application.Regulatory.Commands;

public sealed record DownloadRegulatoryExportCommand(Guid ExportId, Guid RequestedByUserId)
    : IRequest<Result<RegulatoryExportDownloadDto>>;

public sealed record RegulatoryExportDownloadDto(byte[] Bytes, string ContentType, string FileName, string Sha256);

public sealed class DownloadRegulatoryExportCommandHandler(
    IRegulatoryExportRepository exportRepository,
    IReportAuthorizationService authorizationService,
    IBlobStorageService blobStorage,
    IAuditLogRepository auditLogRepository,
    TelemetryClient telemetryClient,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DownloadRegulatoryExportCommand, Result<RegulatoryExportDownloadDto>>
{
    public async Task<Result<RegulatoryExportDownloadDto>> Handle(DownloadRegulatoryExportCommand request, CancellationToken ct)
    {
        var export = await exportRepository.GetByIdAsync(request.ExportId, ct);
        if (export is null)
            return Result.Failure<RegulatoryExportDownloadDto>("Export no encontrado.");
        if (export.RequestedByUserId != request.RequestedByUserId)
        {
            var authorization = await authorizationService.AuthorizeAsync(
                request.RequestedByUserId, export.Scope, export.Canton, export.OrganizationId, export.ReportType, ct);
            if (!authorization.IsAuthorized)
                return Result.Failure<RegulatoryExportDownloadDto>("Export no encontrado.");
        }
        if (!export.IsDownloadable || string.IsNullOrWhiteSpace(export.BlobUrl))
            return Result.Failure<RegulatoryExportDownloadDto>("El export no está disponible para descarga.");

        var bytes = await blobStorage.DownloadAsync(export.BlobUrl, ct);
        if (bytes is null) return Result.Failure<RegulatoryExportDownloadDto>("El archivo del export no está disponible.");

        var recordResult = export.RecordDownload(DateTimeOffset.UtcNow);
        if (recordResult.IsFailure) return Result.Failure<RegulatoryExportDownloadDto>(recordResult.Errors);
        exportRepository.Update(export);
        await auditLogRepository.AddAsync(AuditLogEntry.Create(
            request.RequestedByUserId,
            AuditAction.RegulatoryExportDownloaded,
            "RegulatoryExport",
            export.Id.ToString()), ct);
        await unitOfWork.SaveChangesAsync(ct);
        telemetryClient.TrackEvent("RegulatoryExport.Downloaded", new Dictionary<string, string>
        {
            ["format"] = export.Format.ToString(),
        }, new Dictionary<string, double>
        {
            ["payloadBytes"] = bytes.Length,
        });

        var extension = export.Format.ToString().ToLowerInvariant();
        var contentType = export.Format switch
        {
            ExportFormat.Json => "application/json",
            ExportFormat.Csv => "text/csv; charset=utf-8",
            _ => "application/octet-stream",
        };
        return Result.Success(new RegulatoryExportDownloadDto(
            bytes,
            contentType,
            $"{export.ExportCode}.{extension}",
            export.PayloadSha256 ?? string.Empty));
    }
}

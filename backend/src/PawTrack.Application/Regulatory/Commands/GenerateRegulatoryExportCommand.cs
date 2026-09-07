using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Regulatory.Dtos;
using PawTrack.Application.Regulatory.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;
using PawTrack.Domain.Regulatory;
using System.Security.Cryptography;
using Microsoft.ApplicationInsights;

namespace PawTrack.Application.Regulatory.Commands;

public sealed record GenerateRegulatoryExportCommand(Guid ExportId, Guid RequestedByUserId)
    : IRequest<Result<RegulatoryExportDto>>;

public sealed class GenerateRegulatoryExportCommandHandler(
    IRegulatoryExportRepository exportRepository,
    IReportAuthorizationService authorizationService,
    IRegulatoryReportQueryService reportQueryService,
    IRegulatoryExportRenderer renderer,
    IBlobStorageService blobStorage,
    IAuditLogRepository auditLogRepository,
    TelemetryClient telemetryClient,
    IUnitOfWork unitOfWork)
    : IRequestHandler<GenerateRegulatoryExportCommand, Result<RegulatoryExportDto>>
{
    private const string Container = "regulatory-exports";

    public async Task<Result<RegulatoryExportDto>> Handle(GenerateRegulatoryExportCommand request, CancellationToken ct)
    {
        var export = await exportRepository.GetByIdAsync(request.ExportId, ct);
        if (export is null)
            return Result.Failure<RegulatoryExportDto>("Export no encontrado.");
        var authorization = await authorizationService.AuthorizeAsync(
            request.RequestedByUserId,
            export.Scope,
            export.Canton,
            export.OrganizationId,
            export.ReportType,
            ct);
        if (!authorization.IsAuthorized)
            return Result.Failure<RegulatoryExportDto>("Export no encontrado.");
        if (export.Status != RegulatoryExportStatus.Requested)
            return Result.Failure<RegulatoryExportDto>("El export no está pendiente de generación.");
        var startedAt = DateTimeOffset.UtcNow;
        export.Start(startedAt);
        exportRepository.Update(export);
        await auditLogRepository.AddAsync(AuditLogEntry.Create(
            request.RequestedByUserId,
            AuditAction.RegulatoryExportStarted,
            "RegulatoryExport",
            export.Id.ToString()), ct);
        await unitOfWork.SaveChangesAsync(ct);

        try
        {
            var reportData = await reportQueryService.GetReportDataAsync(
                export.ReportType,
                export.PeriodStart,
                export.PeriodEnd,
                new RegulatoryReportFilter(export.Canton, null, null, export.OrganizationId),
                ct);
            var payloadResult = renderer.Render(export.Format, reportData);
            if (payloadResult.IsFailure)
            {
                await FailAsync(export, payloadResult.Errors[0], ct);
                return Result.Failure<RegulatoryExportDto>(payloadResult.Errors);
            }

            var payload = payloadResult.Value!;
            var hash = Convert.ToHexString(SHA256.HashData(payload.Bytes)).ToLowerInvariant();
            var blobName = $"{export.Scope}/{export.ReportType}/{export.PeriodStart:yyyy}/{export.PeriodStart:MM}/{export.Id}/{export.ExportCode}.{payload.FileExtension}";
            await using var stream = new MemoryStream(payload.Bytes);
            var blobUrl = await blobStorage.UploadAsync(Container, blobName, stream, payload.ContentType, ct);
            export.Complete(
                rowCount: reportData.Rows.Count,
                suppressedRowCount: reportData.Rows.Count(row => row.IsSuppressed),
                hash,
                blobUrl,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow.AddDays(30));
            exportRepository.Update(export);
            await auditLogRepository.AddAsync(AuditLogEntry.Create(
                request.RequestedByUserId,
                AuditAction.RegulatoryExportCompleted,
                "RegulatoryExport",
                export.Id.ToString(),
                $"rows={export.RowCount};suppressed={export.SuppressedRowCount}"), ct);
            await unitOfWork.SaveChangesAsync(ct);
            telemetryClient.TrackEvent("RegulatoryExport.Completed", new Dictionary<string, string>
            {
                ["reportType"] = export.ReportType.ToString(),
                ["format"] = export.Format.ToString(),
            }, new Dictionary<string, double>
            {
                ["rowCount"] = export.RowCount ?? 0,
                ["suppressedRowCount"] = export.SuppressedRowCount ?? 0,
                ["payloadBytes"] = payload.Bytes.Length,
            });
            return Result.Success(export.ToDto());
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            await FailAsync(export, "export_generation_failed", ct);
            return Result.Failure<RegulatoryExportDto>("No fue posible generar el export.");
        }
    }

    private async Task FailAsync(RegulatoryExport export, string errorCode, CancellationToken ct)
    {
        export.Fail(errorCode);
        exportRepository.Update(export);
        await auditLogRepository.AddAsync(AuditLogEntry.Create(
            export.RequestedByUserId,
            AuditAction.RegulatoryExportFailed,
            "RegulatoryExport",
            export.Id.ToString(),
            errorCode), ct);
        await unitOfWork.SaveChangesAsync(ct);
        telemetryClient.TrackEvent("RegulatoryExport.Failed", new Dictionary<string, string>
        {
            ["reportType"] = export.ReportType.ToString(),
            ["errorCode"] = errorCode,
        });
    }
}

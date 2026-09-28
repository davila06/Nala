using Microsoft.Extensions.Logging;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Medical;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Medical;

namespace PawTrack.Infrastructure.Medical;

public sealed class HealthReportExportJob(
    IHealthReportExportRepository exportRepository,
    IPetRepository petRepository,
    IFamilyRepository familyRepository,
    ISubscriptionService subscriptionService,
    IHealthTimelineReadRepository timelineRepository,
    IConsolidatedHealthPdfGenerator pdfGenerator,
    IBlobStorageService blobStorage,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    ILogger<HealthReportExportJob> logger)
{
    private const int MaxEvents = 50_000;
    private static readonly TimeSpan ExportLifetime = TimeSpan.FromHours(24);

    public async Task ExecuteAsync(CancellationToken ct)
    {
        await ExpireOldExportsAsync(ct);
        await RecoverInterruptedExportsAsync(ct);
        var queued = await exportRepository.GetQueuedAsync(1, ct);
        if (queued.Count == 0) return;
        await ProcessAsync(queued[0], ct);
    }

    private async Task ProcessAsync(HealthReportExport export, CancellationToken ct)
    {
        if (!export.Start(DateTimeOffset.UtcNow)) return;
        exportRepository.Update(export);
        await unitOfWork.SaveChangesAsync(ct);
        try
        {
            var pet = await petRepository.GetByIdAsync(export.PetId, ct);
            var canAccess = pet is not null && (pet.OwnerId == export.RequestedByUserId ||
                (await familyRepository.GetActiveMemberIdsAsync(pet.OwnerId, ct)).Contains(export.RequestedByUserId));
            if (!canAccess || !await subscriptionService.IsFamiliaAsync(export.RequestedByUserId, ct))
            {
                await FailAsync(export, "authorization_revoked", ct);
                return;
            }

            var page = await timelineRepository.GetPageAsync(pet!.Id, 0, MaxEvents + 1, ct);
            if (page.HasMore || page.Items.Count > MaxEvents)
            {
                await FailAsync(export, "report_limit_exceeded", ct);
                return;
            }

            var pdf = await pdfGenerator.GenerateAsync(
                new ConsolidatedHealthReportData(pet.Name, DateTimeOffset.UtcNow, page.Items), ct);
            var blobName = $"{export.RequestedByUserId:N}/{export.PetId:N}/{export.Id:N}.pdf";
            await using var stream = new MemoryStream(pdf);
            var blobUrl = await blobStorage.UploadAsync("medical-health-exports", blobName, stream, "application/pdf", ct);
            if (!export.Complete(blobUrl, page.Items.Count, DateTimeOffset.UtcNow))
            {
                await blobStorage.DeleteAsync(blobUrl, ct);
                await FailAsync(export, "invalid_export_state", ct);
                return;
            }

            exportRepository.Update(export);
            await auditLogRepository.AddAsync(AuditLogEntry.Create(
                export.RequestedByUserId, AuditAction.MedicalHealthReportCompleted,
                "HealthReportExport", export.Id.ToString(), $"items={page.Items.Count}"), ct);
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Medical health report export {ExportId} failed", export.Id);
            await FailAsync(export, "report_generation_failed", ct);
        }
    }

    private async Task FailAsync(HealthReportExport export, string code, CancellationToken ct)
    {
        if (!export.Fail(code)) return;
        exportRepository.Update(export);
        await auditLogRepository.AddAsync(AuditLogEntry.Create(
            export.RequestedByUserId, AuditAction.MedicalHealthReportFailed,
            "HealthReportExport", export.Id.ToString(), code), ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task RecoverInterruptedExportsAsync(CancellationToken ct)
    {
        var stale = await exportRepository.GetStaleProcessingAsync(DateTimeOffset.UtcNow.AddHours(-2), 20, ct);
        foreach (var export in stale)
        {
            if (!export.RecoverInterrupted(DateTimeOffset.UtcNow.AddHours(-2))) continue;
            exportRepository.Update(export);
            if (export.Status == HealthReportExportStatus.Failed)
                await auditLogRepository.AddAsync(AuditLogEntry.Create(
                    export.RequestedByUserId, AuditAction.MedicalHealthReportFailed,
                    "HealthReportExport", export.Id.ToString(), export.ErrorCode), ct);
        }
        if (stale.Count > 0) await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task ExpireOldExportsAsync(CancellationToken ct)
    {
        var expired = await exportRepository.GetExpiredAsync(DateTimeOffset.UtcNow, 20, ct);
        foreach (var export in expired)
        {
            var blobUrl = export.BlobUrl;
            if (!export.Expire(DateTimeOffset.UtcNow)) continue;
            if (!string.IsNullOrWhiteSpace(blobUrl)) await blobStorage.DeleteAsync(blobUrl, ct);
            exportRepository.Update(export);
            await auditLogRepository.AddAsync(AuditLogEntry.Create(
                export.RequestedByUserId, AuditAction.MedicalHealthReportExpired,
                "HealthReportExport", export.Id.ToString()), ct);
        }
        if (expired.Count > 0) await unitOfWork.SaveChangesAsync(ct);
    }
}

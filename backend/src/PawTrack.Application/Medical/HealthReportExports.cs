using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;
using PawTrack.Domain.Medical;

namespace PawTrack.Application.Medical;

public sealed record HealthReportExportDto(Guid Id, Guid PetId, string Status, DateTimeOffset RequestedAt,
    DateTimeOffset? CompletedAt, DateTimeOffset ExpiresAt, int? ItemCount, string? ErrorCode)
{
    public static HealthReportExportDto FromDomain(HealthReportExport export) => new(
        export.Id, export.PetId, export.Status.ToString(), export.RequestedAt, export.CompletedAt,
        export.ExpiresAt, export.ItemCount, export.ErrorCode);
}

public sealed record RequestHealthReportExportCommand(Guid PetId, Guid RequestingUserId)
    : IRequest<Result<HealthReportExportDto>>;

public sealed class RequestHealthReportExportCommandHandler(
    IPetRepository petRepository,
    IFamilyRepository familyRepository,
    ISubscriptionService subscriptionService,
    IHealthReportExportRepository exportRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RequestHealthReportExportCommand, Result<HealthReportExportDto>>
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    public async Task<Result<HealthReportExportDto>> Handle(RequestHealthReportExportCommand request, CancellationToken ct)
    {
        if (!await subscriptionService.IsFamiliaAsync(request.RequestingUserId, ct))
            return Result.Failure<HealthReportExportDto>("El reporte consolidado requiere el plan Familia.");

        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null || pet.OwnerId != request.RequestingUserId &&
            !(await familyRepository.GetActiveMemberIdsAsync(pet.OwnerId, ct)).Contains(request.RequestingUserId))
            return Result.Failure<HealthReportExportDto>("Acceso denegado.");

        var existing = await exportRepository.GetReusableAsync(request.RequestingUserId, pet.Id, DateTimeOffset.UtcNow, ct);
        if (existing is not null) return Result.Success(HealthReportExportDto.FromDomain(existing));

        var export = HealthReportExport.Queue(pet.Id, request.RequestingUserId, Lifetime);
        await exportRepository.AddAsync(export, ct);
        await auditLogRepository.AddAsync(AuditLogEntry.Create(
            request.RequestingUserId, AuditAction.MedicalHealthReportRequested,
            "HealthReportExport", export.Id.ToString()), ct);
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var winningExport = await exportRepository.GetReusableAsync(
                request.RequestingUserId, pet.Id, DateTimeOffset.UtcNow, ct);
            if (winningExport is not null) return Result.Success(HealthReportExportDto.FromDomain(winningExport));
            throw;
        }
        return Result.Success(HealthReportExportDto.FromDomain(export));
    }
}

public sealed record GetHealthReportExportQuery(Guid ExportId, Guid RequestingUserId)
    : IRequest<Result<HealthReportExportDto>>;

public sealed class GetHealthReportExportQueryHandler(
    IHealthReportExportRepository repository,
    IPetRepository petRepository,
    IFamilyRepository familyRepository,
    ISubscriptionService subscriptionService)
    : IRequestHandler<GetHealthReportExportQuery, Result<HealthReportExportDto>>
{
    public async Task<Result<HealthReportExportDto>> Handle(GetHealthReportExportQuery request, CancellationToken ct)
    {
        var export = await repository.GetByIdAsync(request.ExportId, ct);
        if (export is null || export.RequestedByUserId != request.RequestingUserId ||
            !await IsStillAuthorizedAsync(export, request.RequestingUserId, ct))
            return Result.Failure<HealthReportExportDto>("Export no encontrado.");
        return Result.Success(HealthReportExportDto.FromDomain(export));
    }

    private async Task<bool> IsStillAuthorizedAsync(HealthReportExport export, Guid userId, CancellationToken ct)
    {
        if (!await subscriptionService.IsFamiliaAsync(userId, ct)) return false;
        var pet = await petRepository.GetByIdAsync(export.PetId, ct);
        return pet is not null && (pet.OwnerId == userId ||
            (await familyRepository.GetActiveMemberIdsAsync(pet.OwnerId, ct)).Contains(userId));
    }
}

public sealed record DownloadHealthReportExportQuery(Guid ExportId, Guid RequestingUserId)
    : IRequest<Result<MedicalDocumentDownloadDto>>;

public sealed class DownloadHealthReportExportQueryHandler(
    IHealthReportExportRepository repository,
    IPetRepository petRepository,
    IFamilyRepository familyRepository,
    ISubscriptionService subscriptionService,
    IBlobStorageService blobStorage,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DownloadHealthReportExportQuery, Result<MedicalDocumentDownloadDto>>
{
    public async Task<Result<MedicalDocumentDownloadDto>> Handle(DownloadHealthReportExportQuery request, CancellationToken ct)
    {
        var export = await repository.GetByIdAsync(request.ExportId, ct);
        if (export is null || export.RequestedByUserId != request.RequestingUserId ||
            !await IsStillAuthorizedAsync(export, request.RequestingUserId, ct) || !export.IsDownloadable ||
            string.IsNullOrWhiteSpace(export.BlobUrl) || !IsHealthExportBlob(export.BlobUrl))
            return Result.Failure<MedicalDocumentDownloadDto>("Export no encontrado.");

        var bytes = await blobStorage.DownloadAsync(export.BlobUrl, ct);
        if (bytes is null) return Result.Failure<MedicalDocumentDownloadDto>("Export no encontrado.");
        await auditLogRepository.AddAsync(AuditLogEntry.Create(
            request.RequestingUserId, AuditAction.MedicalHealthReportDownloaded,
            "HealthReportExport", export.Id.ToString(), $"items={export.ItemCount ?? 0}"), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(new MedicalDocumentDownloadDto(bytes, "application/pdf", $"historial-consolidado-{export.PetId:N}.pdf"));
    }

    private async Task<bool> IsStillAuthorizedAsync(HealthReportExport export, Guid userId, CancellationToken ct)
    {
        if (!await subscriptionService.IsFamiliaAsync(userId, ct)) return false;
        var pet = await petRepository.GetByIdAsync(export.PetId, ct);
        return pet is not null && (pet.OwnerId == userId ||
            (await familyRepository.GetActiveMemberIdsAsync(pet.OwnerId, ct)).Contains(userId));
    }

    private static bool IsHealthExportBlob(string blobUrl)
    {
        if (!Uri.TryCreate(blobUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps) return false;
        var segments = uri.AbsolutePath.TrimStart('/').Split('/', 2);
        return segments.Length == 2 && string.Equals(Uri.UnescapeDataString(segments[0]),
            "medical-health-exports", StringComparison.OrdinalIgnoreCase);
    }
}

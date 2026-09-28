using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Subscriptions.Services;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Common;
using PawTrack.Domain.Medical;

namespace PawTrack.Application.Medical;

public sealed record MedicalDocumentDownloadDto(byte[] Bytes, string ContentType, string FileName);

public sealed record DownloadMedicalDocumentQuery(Guid PetId, Guid RecordId, Guid RequestingUserId, Guid? ClinicId = null)
    : IRequest<Result<MedicalDocumentDownloadDto>>;

public sealed class DownloadMedicalDocumentQueryHandler(
    IMedicalRepository medicalRepository,
    IPetRepository petRepository,
    IFamilyRepository familyRepository,
    ISubscriptionService subscriptionService,
    IClinicRepository clinicRepository,
    IClinicScanRepository clinicScanRepository,
    IClinicMedicalAccessGrantRepository grantRepository,
    IClinicMedicalAccessLogRepository clinicAccessLogRepository,
    IBlobStorageService blobStorage,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DownloadMedicalDocumentQuery, Result<MedicalDocumentDownloadDto>>
{
    public async Task<Result<MedicalDocumentDownloadDto>> Handle(DownloadMedicalDocumentQuery request, CancellationToken ct)
    {
        var record = await medicalRepository.GetByIdAsync(request.RecordId, ct);
        if (record is null || record.PetId != request.PetId || string.IsNullOrWhiteSpace(record.DocumentUrl))
            return Result.Failure<MedicalDocumentDownloadDto>("Documento no encontrado.");

        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null) return Result.Failure<MedicalDocumentDownloadDto>("Documento no encontrado.");

        var accessMethod = "owner_or_family";
        if (request.ClinicId is { } clinicId)
        {
            var clinic = await clinicRepository.GetByIdAsync(clinicId, ct);
            if (clinic is null || clinic.UserId != request.RequestingUserId || clinic.Status != PawTrack.Domain.Clinics.ClinicStatus.Active)
                return Result.Failure<MedicalDocumentDownloadDto>("Acceso denegado.");

            if (await clinicScanRepository.HasRecentScanAsync(clinicId, pet.Id, 90, ct))
            {
                accessMethod = "recent_scan";
            }
            else if (await grantRepository.GetActiveGrantAsync(clinicId, pet.Id, ct)
                is { } grant && grant.HasPermission(ClinicMedicalAccessPermission.Read))
            {
                accessMethod = "active_grant";
            }
            else
            {
                return Result.Failure<MedicalDocumentDownloadDto>("Acceso denegado.");
            }

            await clinicAccessLogRepository.AddAsync(ClinicMedicalAccessLog.Create(
                pet.Id, clinicId, request.RequestingUserId, "download_medical_document",
                ClinicMedicalAccessPermission.Read, accessMethod, "allowed"), ct);
        }
        else
        {
            var canAccess = pet.OwnerId == request.RequestingUserId ||
                (await familyRepository.GetActiveMemberIdsAsync(pet.OwnerId, ct)).Contains(request.RequestingUserId);
            if (!canAccess || !await subscriptionService.IsFamiliaAsync(request.RequestingUserId, ct))
                return Result.Failure<MedicalDocumentDownloadDto>("Acceso denegado.");
        }

        if (!IsMedicalDocumentBlob(record.DocumentUrl))
            return Result.Failure<MedicalDocumentDownloadDto>("Documento no encontrado.");
        var bytes = await blobStorage.DownloadAsync(record.DocumentUrl, ct);
        if (bytes is null) return Result.Failure<MedicalDocumentDownloadDto>("Documento no encontrado.");

        await auditLogRepository.AddAsync(AuditLogEntry.Create(
            request.RequestingUserId, AuditAction.MedicalDocumentDownloaded,
            "MedicalRecord", record.Id.ToString(), $"actor={(request.ClinicId.HasValue ? "clinic" : "owner_or_family")};method={accessMethod}"), ct);
        await unitOfWork.SaveChangesAsync(ct);

        var contentType = record.DocumentContentType ?? ResolveLegacyContentType(record.DocumentUrl);
        var extension = contentType switch
        {
            "application/pdf" => "pdf",
            "image/png" => "png",
            "image/jpeg" => "jpg",
            _ => "bin",
        };
        return Result.Success(new MedicalDocumentDownloadDto(bytes, contentType, $"medical-document-{record.Id:N}.{extension}"));
    }

    private static bool IsMedicalDocumentBlob(string blobUrl)
    {
        if (!Uri.TryCreate(blobUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps) return false;
        var path = uri.AbsolutePath.TrimStart('/');
        const string publicPrefix = "api/public/media/";
        if (path.StartsWith(publicPrefix, StringComparison.OrdinalIgnoreCase)) path = path[publicPrefix.Length..];
        var slash = path.IndexOf('/');
        return slash > 0 && string.Equals(Uri.UnescapeDataString(path[..slash]), "medical-docs", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveLegacyContentType(string blobUrl) =>
        Uri.TryCreate(blobUrl, UriKind.Absolute, out var uri) && uri.AbsolutePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
            ? "application/pdf"
            : "application/octet-stream";
}

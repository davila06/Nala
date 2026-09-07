using MediatR;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Certificates.Queries.DownloadCertificatePdf;

public sealed record DownloadCertificatePdfQuery(Guid CertificateId, Guid RequestingUserId, bool IsAdmin)
    : IRequest<Result<CertificatePdfDownloadDto>>;

public sealed record CertificatePdfDownloadDto(byte[] Bytes, string FileName, string ContentType);

public sealed class DownloadCertificatePdfQueryHandler(
    ICertificateRepository certificateRepository,
    IPetRepository petRepository,
    IFamilyRepository familyRepository,
    IClinicRepository clinicRepository,
    IBlobStorageService blobStorage,
    ICertificateAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DownloadCertificatePdfQuery, Result<CertificatePdfDownloadDto>>
{
    public async Task<Result<CertificatePdfDownloadDto>> Handle(
        DownloadCertificatePdfQuery request,
        CancellationToken cancellationToken)
    {
        var certificate = await certificateRepository.GetByIdAsync(request.CertificateId, cancellationToken);
        if (certificate is null)
            return Result.Failure<CertificatePdfDownloadDto>("Certificado no encontrado.");

        if (string.IsNullOrWhiteSpace(certificate.PdfUrl))
            return Result.Failure<CertificatePdfDownloadDto>("El PDF del certificado no está disponible.");

        if (!request.IsAdmin && !await CanDownloadAsync(certificate.PetId, certificate.ClinicId, request.RequestingUserId, cancellationToken))
            return Result.Failure<CertificatePdfDownloadDto>("Acceso denegado.");

        var bytes = await blobStorage.DownloadAsync(certificate.PdfUrl, cancellationToken);
        if (bytes is null)
            return Result.Failure<CertificatePdfDownloadDto>("El PDF del certificado no está disponible.");

        await auditLogRepository.AddAsync(
            CertificateAuditLog.Create(certificate.Id, CertificateAuditAction.Downloaded, request.RequestingUserId),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CertificatePdfDownloadDto(
            bytes,
            $"pawtrack-certificate-{certificate.VerificationCode}.pdf",
            "application/pdf"));
    }

    private async Task<bool> CanDownloadAsync(
        Guid petId,
        Guid clinicId,
        Guid requestingUserId,
        CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(clinicId, cancellationToken);
        if (clinic?.UserId == requestingUserId)
            return true;

        var pet = await petRepository.GetByIdAsync(petId, cancellationToken);
        if (pet is null)
            return false;

        if (pet.OwnerId == requestingUserId)
            return true;

        var familyMembers = await familyRepository.GetActiveMemberIdsAsync(pet.OwnerId, cancellationToken);
        return familyMembers.Contains(requestingUserId);
    }
}

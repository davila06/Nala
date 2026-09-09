using MediatR;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Certificates.Queries.VerifyCertificate;

/// <summary>Public query — no auth required. Used by the QR scan on printed certificates.</summary>
public sealed record VerifyCertificateQuery(string VerificationCode) : IRequest<Result<CertificateVerificationDto?>>;

public sealed record CertificateVerificationDto(
    Guid Id,
    string Type,
    string PetName,
    string PetSpecies,
    string ClinicName,
    string VerificationCode,
    DateTimeOffset IssuedAt,
    DateTimeOffset? ValidUntil,
    bool IsRevoked,
    bool IsValid,
    bool SignatureVerified = false);

public sealed class VerifyCertificateQueryHandler(
    ICertificateRepository certificateRepository,
    IVaccinePassportRepository vaccinePassportRepository,
    ICertificateAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork,
    IBlobStorageService blobStorage,
    ICertificateDigitalSigner digitalSigner)
    : IRequestHandler<VerifyCertificateQuery, Result<CertificateVerificationDto?>>
{
    public async Task<Result<CertificateVerificationDto?>> Handle(
        VerifyCertificateQuery request,
        CancellationToken cancellationToken)
    {
        var cert = await certificateRepository.GetByVerificationCodeAsync(request.VerificationCode, cancellationToken);
        if (cert is null)
            return Result.Success<CertificateVerificationDto?>(null);

        var passport = await vaccinePassportRepository.GetByCertificateIdAsync(cert.Id, cancellationToken);
        await auditLogRepository.AddAsync(
            CertificateAuditLog.Create(cert.Id, CertificateAuditAction.VerifiedPublicly),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var signatureVerified = false;
        if (!string.IsNullOrWhiteSpace(cert.PdfUrl) && !string.IsNullOrWhiteSpace(cert.SignatureUrl))
        {
            var pdf = await blobStorage.DownloadAsync(cert.PdfUrl, cancellationToken);
            var signature = await blobStorage.DownloadAsync(cert.SignatureUrl, cancellationToken);
            if (pdf is not null && signature is not null)
            {
                try
                {
                    signatureVerified = await digitalSigner.VerifyAsync(
                        pdf, DecodeSignature(signature), cancellationToken);
                }
                catch (FormatException)
                {
                    signatureVerified = false;
                }
            }
        }

        return Result.Success<CertificateVerificationDto?>(new CertificateVerificationDto(
            cert.Id,
            cert.Type.ToString(),
            passport?.PetNameSnapshot ?? string.Empty,
            passport?.PetSpeciesSnapshot ?? cert.Type.ToString(),
            passport?.ClinicNameSnapshot ?? string.Empty,
            cert.VerificationCode,
            cert.IssuedAt,
            cert.ValidUntil,
            cert.IsRevoked,
            cert.IsValid,
            signatureVerified));
    }

    private static byte[] DecodeSignature(byte[] storedSignature)
    {
        var encoded = System.Text.Encoding.UTF8.GetString(storedSignature);
        return Convert.FromBase64String(encoded);
    }
}

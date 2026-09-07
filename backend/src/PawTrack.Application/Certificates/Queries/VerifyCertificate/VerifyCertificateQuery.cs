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
    bool IsValid);

public sealed class VerifyCertificateQueryHandler(
    ICertificateRepository certificateRepository,
    IVaccinePassportRepository vaccinePassportRepository,
    ICertificateAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork)
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
            cert.IsValid));
    }
}

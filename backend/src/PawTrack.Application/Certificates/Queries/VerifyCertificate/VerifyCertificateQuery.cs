using MediatR;
using PawTrack.Application.Certificates.DTOs;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Certificates.Queries.VerifyCertificate;

/// <summary>Public query — no auth required. Used by the QR scan on printed certificates.</summary>
public sealed record VerifyCertificateQuery(string VerificationCode) : IRequest<Result<CertificateDto?>>;

public sealed class VerifyCertificateQueryHandler(ICertificateRepository certificateRepository)
    : IRequestHandler<VerifyCertificateQuery, Result<CertificateDto?>>
{
    public async Task<Result<CertificateDto?>> Handle(
        VerifyCertificateQuery request,
        CancellationToken cancellationToken)
    {
        var cert = await certificateRepository.GetByVerificationCodeAsync(request.VerificationCode, cancellationToken);
        return Result.Success<CertificateDto?>(cert is null ? null : CertificateDto.FromDomain(cert));
    }
}

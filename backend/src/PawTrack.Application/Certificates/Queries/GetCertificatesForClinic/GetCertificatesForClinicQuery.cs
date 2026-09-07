using MediatR;
using PawTrack.Application.Certificates.DTOs;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Certificates.Queries.GetCertificatesForClinic;

public sealed record GetCertificatesForClinicQuery(
    Guid ClinicId,
    Guid RequestingUserId,
    bool IsAdmin = false,
    int Page = 1,
    int PageSize = 20)
    : IRequest<Result<IReadOnlyList<CertificateDto>>>;

public sealed class GetCertificatesForClinicQueryHandler(
    ICertificateRepository certificateRepository,
    IClinicRepository clinicRepository)
    : IRequestHandler<GetCertificatesForClinicQuery, Result<IReadOnlyList<CertificateDto>>>
{
    public async Task<Result<IReadOnlyList<CertificateDto>>> Handle(
        GetCertificatesForClinicQuery request,
        CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null)
            return Result.Failure<IReadOnlyList<CertificateDto>>("Clínica no encontrada.");

        if (!request.IsAdmin && clinic.UserId != request.RequestingUserId)
            return Result.Failure<IReadOnlyList<CertificateDto>>("Acceso denegado.");

        var certs = await certificateRepository.GetForClinicAsync(
            request.ClinicId, request.Page, request.PageSize, cancellationToken);

        return Result.Success(certs.Select(CertificateDto.FromDomain).ToList() as IReadOnlyList<CertificateDto>);
    }
}

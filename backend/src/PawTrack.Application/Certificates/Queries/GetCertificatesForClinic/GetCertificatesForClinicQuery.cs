using MediatR;
using PawTrack.Application.Certificates.DTOs;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Certificates.Queries.GetCertificatesForClinic;

public sealed record GetCertificatesForClinicQuery(Guid ClinicId, int Page = 1, int PageSize = 20)
    : IRequest<Result<IReadOnlyList<CertificateDto>>>;

public sealed class GetCertificatesForClinicQueryHandler(ICertificateRepository certificateRepository)
    : IRequestHandler<GetCertificatesForClinicQuery, Result<IReadOnlyList<CertificateDto>>>
{
    public async Task<Result<IReadOnlyList<CertificateDto>>> Handle(
        GetCertificatesForClinicQuery request,
        CancellationToken cancellationToken)
    {
        var certs = await certificateRepository.GetForClinicAsync(
            request.ClinicId, request.Page, request.PageSize, cancellationToken);

        return Result.Success(certs.Select(CertificateDto.FromDomain).ToList() as IReadOnlyList<CertificateDto>);
    }
}

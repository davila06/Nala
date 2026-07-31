using MediatR;
using PawTrack.Application.Certificates.DTOs;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Certificates.Queries.GetCertificatesForPet;

public sealed record GetCertificatesForPetQuery(Guid PetId) : IRequest<Result<IReadOnlyList<CertificateDto>>>;

public sealed class GetCertificatesForPetQueryHandler(ICertificateRepository certificateRepository)
    : IRequestHandler<GetCertificatesForPetQuery, Result<IReadOnlyList<CertificateDto>>>
{
    public async Task<Result<IReadOnlyList<CertificateDto>>> Handle(
        GetCertificatesForPetQuery request,
        CancellationToken cancellationToken)
    {
        var certs = await certificateRepository.GetForPetAsync(request.PetId, cancellationToken);
        return Result.Success(certs.Select(CertificateDto.FromDomain).ToList() as IReadOnlyList<CertificateDto>);
    }
}

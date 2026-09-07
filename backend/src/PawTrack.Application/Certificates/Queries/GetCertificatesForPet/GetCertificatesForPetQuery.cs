using MediatR;
using PawTrack.Application.Certificates.DTOs;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Certificates.Queries.GetCertificatesForPet;

public sealed record GetCertificatesForPetQuery(Guid PetId, Guid RequestingUserId, bool IsAdmin = false)
    : IRequest<Result<IReadOnlyList<CertificateDto>>>;

public sealed class GetCertificatesForPetQueryHandler(
    ICertificateRepository certificateRepository,
    IPetRepository petRepository,
    IFamilyRepository familyRepository)
    : IRequestHandler<GetCertificatesForPetQuery, Result<IReadOnlyList<CertificateDto>>>
{
    public async Task<Result<IReadOnlyList<CertificateDto>>> Handle(
        GetCertificatesForPetQuery request,
        CancellationToken cancellationToken)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken);
        if (pet is null)
            return Result.Failure<IReadOnlyList<CertificateDto>>("Mascota no encontrada.");

        if (!request.IsAdmin && pet.OwnerId != request.RequestingUserId)
        {
            var familyMembers = await familyRepository.GetActiveMemberIdsAsync(pet.OwnerId, cancellationToken);
            if (!familyMembers.Contains(request.RequestingUserId))
                return Result.Failure<IReadOnlyList<CertificateDto>>("Acceso denegado.");
        }

        var certs = await certificateRepository.GetForPetAsync(request.PetId, cancellationToken);
        return Result.Success(certs.Select(CertificateDto.FromDomain).ToList() as IReadOnlyList<CertificateDto>);
    }
}

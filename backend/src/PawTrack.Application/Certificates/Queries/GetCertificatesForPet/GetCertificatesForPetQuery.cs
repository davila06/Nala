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

public sealed record CertificatePageDto(IReadOnlyList<CertificateDto> Items, bool HasMore);

public sealed record GetCertificatesForPetPageQuery(Guid PetId, Guid RequestingUserId, bool IsAdmin, int Page, int PageSize)
    : IRequest<Result<CertificatePageDto>>;

public sealed class GetCertificatesForPetPageQueryHandler(
    ICertificateRepository certificateRepository, IPetRepository petRepository, IFamilyRepository familyRepository)
    : IRequestHandler<GetCertificatesForPetPageQuery, Result<CertificatePageDto>>
{
    public async Task<Result<CertificatePageDto>> Handle(GetCertificatesForPetPageQuery request, CancellationToken ct)
    {
        if (request.PetId == Guid.Empty || request.Page < 1 || request.PageSize is < 1 or > 50 ||
            (long)(request.Page - 1) * request.PageSize > int.MaxValue - request.PageSize - 1)
            return Result.Failure<CertificatePageDto>("Paginación inválida.");

        var pet = await petRepository.GetByIdAsync(request.PetId, ct);
        if (pet is null) return Result.Failure<CertificatePageDto>("Mascota no encontrada.");
        if (!request.IsAdmin && pet.OwnerId != request.RequestingUserId &&
            !(await familyRepository.GetActiveMemberIdsAsync(pet.OwnerId, ct)).Contains(request.RequestingUserId))
            return Result.Failure<CertificatePageDto>("Acceso denegado.");

        var certificates = await certificateRepository.GetForPetPageAsync(pet.Id,
            (request.Page - 1) * request.PageSize, request.PageSize + 1, ct);
        return Result.Success(new CertificatePageDto(
            certificates.Take(request.PageSize).Select(CertificateDto.FromDomain).ToList(),
            certificates.Count > request.PageSize));
    }
}

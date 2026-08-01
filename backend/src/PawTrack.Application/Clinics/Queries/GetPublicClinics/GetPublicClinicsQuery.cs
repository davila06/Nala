using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Clinics.Queries.GetPublicClinics;

public sealed record PublicClinicDto(
    Guid Id,
    string Name,
    string Address,
    string ContactEmail,
    string? PhoneNumber,
    string? Website,
    string? LogoUrl,
    decimal Lat,
    decimal Lng,
    bool IsFeatured,
    string Status)
{
    public static PublicClinicDto FromDomain(Clinic c) => new(
        c.Id, c.Name, c.Address, c.ContactEmail,
        c.PhoneNumber, c.Website, c.LogoUrl,
        c.Lat, c.Lng, c.IsFeatured, c.Status.ToString());
}

public sealed record GetPublicClinicsQuery(double? Lat, double? Lng, double RadiusKm = 80)
    : IRequest<Result<IReadOnlyList<PublicClinicDto>>>;

public sealed class GetPublicClinicsQueryHandler(IClinicRepository clinicRepository)
    : IRequestHandler<GetPublicClinicsQuery, Result<IReadOnlyList<PublicClinicDto>>>
{
    public async Task<Result<IReadOnlyList<PublicClinicDto>>> Handle(
        GetPublicClinicsQuery request, CancellationToken cancellationToken)
    {
        var clinics = await clinicRepository.GetAllActiveAsync(cancellationToken);

        IReadOnlyList<PublicClinicDto> result = clinics
            .Select(PublicClinicDto.FromDomain)
            .ToList()
            .AsReadOnly();

        return Result.Success(result);
    }
}

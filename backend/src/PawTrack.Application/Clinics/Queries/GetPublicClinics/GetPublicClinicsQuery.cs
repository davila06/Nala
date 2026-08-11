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
    bool IsEmergency24h,
    string? EmergencyPhone,
    string Status)
{
    public static PublicClinicDto FromDomain(Clinic c) => new(
        c.Id, c.Name, c.Address, c.ContactEmail,
        c.PhoneNumber, c.Website, c.LogoUrl,
        c.Lat, c.Lng, c.IsFeatured,
        c.IsEmergency24h, c.EmergencyPhone, c.Status.ToString());
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

// ── Emergency vets ────────────────────────────────────────────────────────────

public sealed record EmergencyVetDto(
    Guid Id,
    string Name,
    string Address,
    string? EmergencyPhone,
    string? PhoneNumber,
    string? Website,
    string? LogoUrl,
    decimal Lat,
    decimal Lng,
    double? DistanceKm)
{
    public static EmergencyVetDto FromDomain(Clinic c, double? distanceKm) => new(
        c.Id, c.Name, c.Address,
        c.EmergencyPhone, c.PhoneNumber,
        c.Website, c.LogoUrl,
        c.Lat, c.Lng, distanceKm);
}

public sealed record GetEmergencyVetsQuery(double? UserLat, double? UserLng, double RadiusKm = 30)
    : IRequest<Result<IReadOnlyList<EmergencyVetDto>>>;

public sealed class GetEmergencyVetsQueryHandler(IClinicRepository clinicRepository)
    : IRequestHandler<GetEmergencyVetsQuery, Result<IReadOnlyList<EmergencyVetDto>>>
{
    public async Task<Result<IReadOnlyList<EmergencyVetDto>>> Handle(
        GetEmergencyVetsQuery request, CancellationToken ct)
    {
        var clinics = await clinicRepository.GetAllActiveAsync(ct);
        var emergency = clinics.Where(c => c.IsEmergency24h).ToList();

        IReadOnlyList<EmergencyVetDto> result;

        if (request.UserLat.HasValue && request.UserLng.HasValue)
        {
            result = emergency
                .Select(c => EmergencyVetDto.FromDomain(c, Haversine(
                    request.UserLat.Value, request.UserLng.Value,
                    (double)c.Lat, (double)c.Lng)))
                .OrderBy(c => c.DistanceKm)
                .Where(c => c.DistanceKm <= request.RadiusKm)
                .Take(5)
                .ToList()
                .AsReadOnly();
        }
        else
        {
            result = emergency
                .Select(c => EmergencyVetDto.FromDomain(c, null))
                .Take(5)
                .ToList()
                .AsReadOnly();
        }

        return Result.Success(result);
    }

    private static double Haversine(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371.0;
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLng = (lng2 - lng1) * Math.PI / 180.0;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0)
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}

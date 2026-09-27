using MediatR;
using PawTrack.Application.Clinics.DTOs;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Clinics.Queries.GetMyClinic;

public sealed record GetMyClinicQuery(Guid UserId) : IRequest<Result<ClinicDto?>>;

public sealed class GetMyClinicQueryHandler(
    IClinicRepository clinicRepository,
    IActiveClinicSiteContext siteContext)
    : IRequestHandler<GetMyClinicQuery, Result<ClinicDto?>>
{
    public async Task<Result<ClinicDto?>> Handle(
        GetMyClinicQuery request,
        CancellationToken cancellationToken)
    {
        if (siteContext.UserId.HasValue && siteContext.UserId != request.UserId)
            return Result.Failure<ClinicDto?>("Acceso denegado.");

        var clinic = siteContext.ClinicId.HasValue
            ? await clinicRepository.GetByIdAsync(siteContext.ClinicId.Value, cancellationToken)
            : null;
        return Result.Success(clinic is null || clinic.UserId != request.UserId
            ? null
            : ClinicDto.FromDomain(clinic));
    }
}

using MediatR;
using PawTrack.Application.Clinics.DTOs;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Clinics.Queries.GetMyClinic;

public sealed record GetMyClinicQuery(Guid UserId) : IRequest<Result<ClinicDto?>>;

public sealed class GetMyClinicQueryHandler(IClinicRepository clinicRepository)
    : IRequestHandler<GetMyClinicQuery, Result<ClinicDto?>>
{
    public async Task<Result<ClinicDto?>> Handle(
        GetMyClinicQuery request,
        CancellationToken cancellationToken)
    {
        var clinic = await clinicRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        return Result.Success(clinic is null ? null : ClinicDto.FromDomain(clinic));
    }
}

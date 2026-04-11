using MediatR;
using PawTrack.Application.Clinics.DTOs;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Clinics.Queries.GetPendingClinics;

public sealed class GetPendingClinicsQueryHandler(IClinicRepository clinicRepository)
    : IRequestHandler<GetPendingClinicsQuery, Result<IReadOnlyList<ClinicDto>>>
{
    /// <summary>
    /// Hard cap on pending-clinic results returned per admin request.
    /// Prevents DoS via memory exhaustion when the pending queue grows unboundedly.
    /// </summary>
    private const int MaxResults = 200;

    public async Task<Result<IReadOnlyList<ClinicDto>>> Handle(
        GetPendingClinicsQuery request,
        CancellationToken cancellationToken)
    {
        var pending = await clinicRepository.GetAllPendingAsync(cancellationToken);
        IReadOnlyList<ClinicDto> dtos = pending.Take(MaxResults).Select(ClinicDto.FromDomain).ToList().AsReadOnly();
        return Result.Success(dtos);
    }
}

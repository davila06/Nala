using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Fosters.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Fosters.Queries.GetMyFosterProfile;

public sealed record GetMyFosterProfileQuery(Guid UserId) : IRequest<Result<FosterProfileDto>>;

public sealed class GetMyFosterProfileQueryHandler(IFosterVolunteerRepository fosterVolunteerRepository)
    : IRequestHandler<GetMyFosterProfileQuery, Result<FosterProfileDto>>
{
    public async Task<Result<FosterProfileDto>> Handle(
        GetMyFosterProfileQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await fosterVolunteerRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (profile is null)
            return Result.Failure<FosterProfileDto>("Foster profile not found.");

        return Result.Success(new FosterProfileDto(
            profile.UserId.ToString(),
            profile.FullName,
            profile.HomeLat,
            profile.HomeLng,
            profile.AcceptedSpecies,
            profile.SizePreference,
            profile.MaxDays,
            profile.IsAvailable,
            profile.AvailableUntil,
            profile.TotalFostersCompleted));
    }
}

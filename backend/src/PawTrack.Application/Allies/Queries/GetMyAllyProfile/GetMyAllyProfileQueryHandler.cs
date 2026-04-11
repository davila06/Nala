using MediatR;
using PawTrack.Application.Allies.DTOs;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Allies.Queries.GetMyAllyProfile;

public sealed class GetMyAllyProfileQueryHandler(IAllyProfileRepository allyProfileRepository)
    : IRequestHandler<GetMyAllyProfileQuery, Result<AllyProfileDto?>>
{
    public async Task<Result<AllyProfileDto?>> Handle(
        GetMyAllyProfileQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await allyProfileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        return Result.Success(profile is null ? null : AllyProfileDto.FromDomain(profile));
    }
}
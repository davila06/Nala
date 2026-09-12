using MediatR;
using PawTrack.Application.Payments.DTOs;
using PawTrack.Application.Payments.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Payments.Queries.GetBillingProfile;

public sealed record GetBillingProfileQuery(Guid UserId)
    : IRequest<Result<UserBillingProfileDto?>>;

public sealed class GetBillingProfileQueryHandler(IUserBillingProfileRepository profileRepository)
    : IRequestHandler<GetBillingProfileQuery, Result<UserBillingProfileDto?>>
{
    public async Task<Result<UserBillingProfileDto?>> Handle(
        GetBillingProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        return Result.Success(profile is null ? null : UserBillingProfileDto.FromDomain(profile));
    }
}

using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Auth;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Auth.Queries.GetMyProfile;

public sealed class GetMyProfileQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetMyProfileQuery, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(
        GetMyProfileQuery request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<UserProfileDto>(["Usuario no encontrado."]);

        return Result.Success(new UserProfileDto(
            user.Id.ToString(),
            user.Name,
            user.Email,
            user.Role == UserRole.Admin));
    }
}

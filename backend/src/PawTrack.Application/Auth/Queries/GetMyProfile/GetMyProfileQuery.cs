using MediatR;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Auth.Queries.GetMyProfile;

public sealed record UserProfileDto(string Id, string Name, string Email, bool IsAdmin);

public sealed record GetMyProfileQuery(Guid UserId) : IRequest<Result<UserProfileDto>>;

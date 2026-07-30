using MediatR;
using PawTrack.Application.Auth.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Auth.Queries.GetMyProfile;

public sealed record GetMyProfileQuery(Guid UserId) : IRequest<Result<UserProfileDto>>;

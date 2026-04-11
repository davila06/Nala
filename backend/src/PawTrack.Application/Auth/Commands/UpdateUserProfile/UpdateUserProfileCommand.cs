using MediatR;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Auth.Commands.UpdateUserProfile;

public sealed record UpdateUserProfileCommand(Guid UserId, string Name) : IRequest<Result<bool>>;

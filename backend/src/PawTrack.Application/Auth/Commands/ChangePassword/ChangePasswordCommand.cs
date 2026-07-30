using MediatR;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Auth.Commands.ChangePassword;

public sealed record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword) : IRequest<Result<bool>>;

using MediatR;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Auth.Commands.ResetPassword;

public sealed record ResetPasswordCommand(string Token, string NewPassword) : IRequest<Result<bool>>;

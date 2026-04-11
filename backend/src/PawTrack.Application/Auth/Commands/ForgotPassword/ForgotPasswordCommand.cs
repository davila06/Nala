using MediatR;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Auth.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : IRequest<Result<bool>>;

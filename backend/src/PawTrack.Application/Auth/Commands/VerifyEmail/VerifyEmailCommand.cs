using MediatR;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Auth.Commands.VerifyEmail;

public sealed record VerifyEmailCommand(string Token) : IRequest<Result<bool>>;

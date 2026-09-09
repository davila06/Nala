using MediatR;
using PawTrack.Application.Auth.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Auth.Commands.Login;

public sealed record LoginCommand(string Email, string Password, string? MfaCode = null) : IRequest<Result<AuthTokenDto>>;

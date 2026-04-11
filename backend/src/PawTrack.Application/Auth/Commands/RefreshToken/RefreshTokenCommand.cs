using MediatR;
using PawTrack.Application.Auth.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string Token) : IRequest<Result<AuthTokenDto>>;

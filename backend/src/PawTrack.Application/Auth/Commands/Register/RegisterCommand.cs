using MediatR;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Name,
    string Email,
    string Password) : IRequest<Result<string>>;

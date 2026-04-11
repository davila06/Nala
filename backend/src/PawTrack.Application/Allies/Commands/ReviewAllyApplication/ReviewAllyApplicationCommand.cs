using MediatR;
using PawTrack.Application.Allies.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Allies.Commands.ReviewAllyApplication;

public sealed record ReviewAllyApplicationCommand(Guid UserId, bool Approve)
    : IRequest<Result<AllyProfileDto>>;
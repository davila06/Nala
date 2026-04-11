using MediatR;
using PawTrack.Application.Allies.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Allies.Queries.GetPendingAllies;

public sealed record GetPendingAlliesQuery : IRequest<Result<IReadOnlyList<AllyProfileDto>>>;

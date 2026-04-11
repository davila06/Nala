using MediatR;
using PawTrack.Application.Allies.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Allies.Queries.GetMyAllyAlerts;

public sealed record GetMyAllyAlertsQuery(Guid UserId) : IRequest<Result<IReadOnlyList<AllyAlertDto>>>;
using MediatR;
using PawTrack.Application.Allies.DTOs;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Allies.Queries.GetMyAllyProfile;

public sealed record GetMyAllyProfileQuery(Guid UserId) : IRequest<Result<AllyProfileDto?>>;
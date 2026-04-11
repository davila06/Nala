using MediatR;
using PawTrack.Application.Allies.DTOs;
using PawTrack.Domain.Allies;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Allies.Commands.SubmitAllyApplication;

public sealed record SubmitAllyApplicationCommand(
    Guid UserId,
    string OrganizationName,
    AllyType AllyType,
    string CoverageLabel,
    double CoverageLat,
    double CoverageLng,
    int CoverageRadiusMetres) : IRequest<Result<AllyProfileDto>>;
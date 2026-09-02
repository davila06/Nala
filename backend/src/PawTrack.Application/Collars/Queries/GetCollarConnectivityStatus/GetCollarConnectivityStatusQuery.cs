using MediatR;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Collars.Queries.GetCollarConnectivityStatus;

public sealed record GetCollarConnectivityStatusQuery(Guid CollarId, Guid RequestingUserId)
    : IRequest<Result<CollarConnectivityStatusDto>>;

public sealed record CollarConnectivityStatusDto(
    Guid CollarId,
    bool IsOffline,
    DateTimeOffset? LastSeenAt,
    int? BatteryPercent,
    bool OfflineAlertsEnabled,
    int OfflineThresholdMinutes,
    bool BatteryAlertsEnabled,
    int BatteryAlertThresholdPercent);

public sealed class GetCollarConnectivityStatusQueryHandler(ICollarRepository collarRepository)
    : IRequestHandler<GetCollarConnectivityStatusQuery, Result<CollarConnectivityStatusDto>>
{
    public async Task<Result<CollarConnectivityStatusDto>> Handle(
        GetCollarConnectivityStatusQuery request, CancellationToken cancellationToken)
    {
        var collar = await collarRepository.GetByIdAsync(request.CollarId, cancellationToken);
        if (collar is null)
            return Result.Failure<CollarConnectivityStatusDto>("Collar no encontrado.");

        if (collar.OwnerId != request.RequestingUserId)
            return Result.Failure<CollarConnectivityStatusDto>("Access denied.");

        return Result.Success(new CollarConnectivityStatusDto(
            collar.Id,
            collar.IsOffline,
            collar.LastSeenAt,
            collar.BatteryPercent,
            collar.OfflineAlertsEnabled,
            collar.OfflineThresholdMinutes,
            collar.BatteryAlertsEnabled,
            collar.BatteryAlertThresholdPercent));
    }
}

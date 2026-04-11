using MediatR;
using PawTrack.Application.Allies.DTOs;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Notifications;

namespace PawTrack.Application.Allies.Queries.GetMyAllyAlerts;

public sealed class GetMyAllyAlertsQueryHandler(
    IAllyProfileRepository allyProfileRepository,
    INotificationRepository notificationRepository)
    : IRequestHandler<GetMyAllyAlertsQuery, Result<IReadOnlyList<AllyAlertDto>>>
{
    /// <summary>
    /// Hard cap on ally alert notifications returned per request.
    /// Prevents DoS via memory exhaustion for accounts with accumulated alerts.
    /// </summary>
    private const int MaxResults = 200;

    public async Task<Result<IReadOnlyList<AllyAlertDto>>> Handle(
        GetMyAllyAlertsQuery request,
        CancellationToken cancellationToken)
    {
        var verifiedProfile = await allyProfileRepository.GetVerifiedByUserIdAsync(request.UserId, cancellationToken);
        if (verifiedProfile is null)
            return Result.Failure<IReadOnlyList<AllyAlertDto>>("Only verified allies can access alert inbox.");

        var notifications = await notificationRepository.GetByUserIdAndTypeAsync(
            request.UserId,
            NotificationType.VerifiedAllyAlert,
            cancellationToken);

        return Result.Success<IReadOnlyList<AllyAlertDto>>(
            notifications.Take(MaxResults).Select(AllyAlertDto.FromDomain).ToList().AsReadOnly());
    }
}
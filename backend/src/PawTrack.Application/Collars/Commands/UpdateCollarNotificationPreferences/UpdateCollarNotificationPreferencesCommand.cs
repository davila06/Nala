using MediatR;
using PawTrack.Application.Collars.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;

namespace PawTrack.Application.Collars.Commands.UpdateCollarNotificationPreferences;

public sealed record UpdateCollarNotificationPreferencesCommand(
    Guid CollarId,
    Guid OwnerId,
    bool OfflineAlertsEnabled,
    int OfflineThresholdMinutes,
    bool BatteryAlertsEnabled,
    int BatteryAlertThresholdPercent) : IRequest<Result<CollarNotificationPreferencesDto>>;

public sealed record CollarNotificationPreferencesDto(
    bool OfflineAlertsEnabled,
    int OfflineThresholdMinutes,
    bool BatteryAlertsEnabled,
    int BatteryAlertThresholdPercent);

public sealed class UpdateCollarNotificationPreferencesCommandHandler(
    ICollarRepository collarRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateCollarNotificationPreferencesCommand, Result<CollarNotificationPreferencesDto>>
{
    public async Task<Result<CollarNotificationPreferencesDto>> Handle(
        UpdateCollarNotificationPreferencesCommand request, CancellationToken cancellationToken)
    {
        var collar = await collarRepository.GetByIdAsync(request.CollarId, cancellationToken);
        if (collar is null || !collar.IsActive)
            return Result.Failure<CollarNotificationPreferencesDto>("Collar no encontrado o inactivo.");

        if (collar.OwnerId != request.OwnerId)
            return Result.Failure<CollarNotificationPreferencesDto>("Access denied.");

        var update = collar.UpdateNotificationPreferences(
            request.OfflineAlertsEnabled,
            request.OfflineThresholdMinutes,
            request.BatteryAlertsEnabled,
            request.BatteryAlertThresholdPercent);

        if (update.IsFailure)
            return Result.Failure<CollarNotificationPreferencesDto>(update.Errors.ToArray());

        collarRepository.Update(collar);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CollarNotificationPreferencesDto(
            collar.OfflineAlertsEnabled,
            collar.OfflineThresholdMinutes,
            collar.BatteryAlertsEnabled,
            collar.BatteryAlertThresholdPercent));
    }
}

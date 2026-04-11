using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Common;
using PawTrack.Domain.Notifications;

namespace PawTrack.Application.Notifications.Commands.UpdateNotificationPreferences;

public sealed class UpdateNotificationPreferencesCommandHandler(
    IUserNotificationPreferencesRepository preferencesRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateNotificationPreferencesCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(
        UpdateNotificationPreferencesCommand request, CancellationToken cancellationToken)
    {
        var prefs = await preferencesRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (prefs is null)
        {
            prefs = UserNotificationPreferences.CreateDefault(request.UserId);
            prefs.UpdatePreventiveAlerts(request.EnablePreventiveAlerts);
            await preferencesRepository.AddAsync(prefs, cancellationToken);
        }
        else
        {
            prefs.UpdatePreventiveAlerts(request.EnablePreventiveAlerts);
            preferencesRepository.Update(prefs);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(Unit.Value);
    }
}

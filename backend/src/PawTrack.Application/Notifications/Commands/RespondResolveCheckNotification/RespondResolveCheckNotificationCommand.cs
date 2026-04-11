using MediatR;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.LostPets.Commands.UpdateLostPetStatus;
using PawTrack.Domain.Common;
using PawTrack.Domain.LostPets;
using PawTrack.Domain.Notifications;
using Microsoft.ApplicationInsights;

namespace PawTrack.Application.Notifications.Commands.RespondResolveCheckNotification;

public sealed record RespondResolveCheckNotificationCommand(
    Guid NotificationId,
    Guid UserId,
    bool FoundAtHome) : IRequest<Result<bool>>;

public sealed class RespondResolveCheckNotificationCommandHandler(
    INotificationRepository notificationRepository,
    ISender sender,
    IUnitOfWork unitOfWork,
    TelemetryClient telemetryClient)
    : IRequestHandler<RespondResolveCheckNotificationCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        RespondResolveCheckNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var notification = await notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);
        if (notification is null)
            return Result.Failure<bool>("Notification not found.");

        if (notification.UserId != request.UserId)
            return Result.Failure<bool>("Access denied.");

        if (notification.Type != NotificationType.ResolveCheck)
            return Result.Failure<bool>("Invalid notification type.");

        if (!Guid.TryParse(notification.RelatedEntityId, out var lostPetEventId))
            return Result.Failure<bool>("Related lost-pet report not found.");

        if (request.FoundAtHome)
        {
            var resolveResult = await sender.Send(
                new UpdateLostPetStatusCommand(
                    lostPetEventId,
                    request.UserId,
                    LostPetStatus.Reunited),
                cancellationToken);

            if (resolveResult.IsFailure)
                return Result.Failure<bool>(resolveResult.Errors);
        }

        var summary = request.FoundAtHome
            ? "Owner confirmed the pet is back home."
            : "Owner confirmed the pet is still missing.";

        notification.ConfirmAction(summary);
        notificationRepository.Update(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Track conversion metric in Application Insights (fire-and-forget, non-blocking)
        var outcome = request.FoundAtHome ? "Confirmed" : "Dismissed";
        telemetryClient.TrackEvent($"ResolveCheck.{outcome}", new Dictionary<string, string>
        {
            ["lostPetEventId"] = lostPetEventId.ToString(),
            ["userId"]         = request.UserId.ToString(),
        });

        return Result.Success(true);
    }
}

using FluentValidation;

namespace PawTrack.Application.Notifications.Commands.RespondResolveCheckNotification;

public sealed class RespondResolveCheckNotificationCommandValidator
    : AbstractValidator<RespondResolveCheckNotificationCommand>
{
    public RespondResolveCheckNotificationCommandValidator()
    {
        RuleFor(x => x.NotificationId)
            .NotEmpty()
            .WithMessage("Notification ID must not be empty.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID must not be empty.");
    }
}

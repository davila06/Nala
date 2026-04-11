using FluentValidation;

namespace PawTrack.Application.Notifications.Commands.RegisterPushSubscription;

public sealed class RegisterPushSubscriptionCommandValidator
    : AbstractValidator<RegisterPushSubscriptionCommand>
{
    public RegisterPushSubscriptionCommandValidator()
    {
        RuleFor(x => x.Endpoint)
            .NotEmpty().WithMessage("Push subscription endpoint is required.")
            .MaximumLength(2048).WithMessage("Push endpoint must not exceed 2048 characters.")
            .Must(e => e.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Push endpoint must use HTTPS.");

        RuleFor(x => x.KeysJson)
            .NotEmpty().WithMessage("Push subscription keys are required.")
            .MaximumLength(1024).WithMessage("Push subscription keys must not exceed 1024 characters.");
    }
}

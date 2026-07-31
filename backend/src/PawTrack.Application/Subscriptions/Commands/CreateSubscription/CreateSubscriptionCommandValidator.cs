using FluentValidation;

namespace PawTrack.Application.Subscriptions.Commands.CreateSubscription;

public sealed class CreateSubscriptionCommandValidator : AbstractValidator<CreateSubscriptionCommand>
{
    public CreateSubscriptionCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => x.UserId.HasValue ^ x.ClinicId.HasValue)
            .WithMessage("Exactly one of UserId or ClinicId must be provided.");
    }
}

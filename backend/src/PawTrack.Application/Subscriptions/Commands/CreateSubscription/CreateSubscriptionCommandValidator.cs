using FluentValidation;
using PawTrack.Domain.Subscriptions;

namespace PawTrack.Application.Subscriptions.Commands.CreateSubscription;

public sealed class CreateSubscriptionCommandValidator : AbstractValidator<CreateSubscriptionCommand>
{
    public CreateSubscriptionCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => x.UserId.HasValue ^ x.ClinicId.HasValue)
            .WithMessage("Exactly one of UserId or ClinicId must be provided.");

        RuleFor(x => x)
            .Must(x => !SubscriptionPricing.IsUserTermTier(x.Tier)
                || SubscriptionPricing.IsSupportedBillingMonths(x.BillingMonths))
            .WithMessage("User plans support billing terms of 1, 3, 6, or 12 months.");
    }
}

using FluentValidation;

namespace PawTrack.Application.Bot.Queries.VerifyWhatsAppWebhook;

public sealed class VerifyWhatsAppWebhookQueryValidator : AbstractValidator<VerifyWhatsAppWebhookQuery>
{
    public VerifyWhatsAppWebhookQueryValidator()
    {
        RuleFor(x => x.HubMode)
            .NotEmpty()
            .WithMessage("Hub mode must not be empty.")
            .MaximumLength(255)
            .WithMessage("Hub mode must not exceed 255 characters.");

        RuleFor(x => x.HubVerifyToken)
            .NotEmpty()
            .WithMessage("Hub verify token must not be empty.")
            .MaximumLength(512)
            .WithMessage("Hub verify token must not exceed 512 characters.");

        RuleFor(x => x.HubChallenge)
            .NotEmpty()
            .WithMessage("Hub challenge must not be empty.")
            .MaximumLength(512)
            .WithMessage("Hub challenge must not exceed 512 characters.");
    }
}

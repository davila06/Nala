using FluentValidation;

namespace PawTrack.Application.Bot.Commands.HandleWhatsAppWebhook;

public sealed class HandleWhatsAppWebhookCommandValidator
    : AbstractValidator<HandleWhatsAppWebhookCommand>
{
    public HandleWhatsAppWebhookCommandValidator()
    {
        // WaId: E.164 without '+' — digits only, 7–24 chars
        RuleFor(x => x.WaId)
            .NotEmpty().WithMessage("WaId is required.")
            .MaximumLength(24).WithMessage("WaId must not exceed 24 characters.")
            .Matches(@"^\d{7,24}$").WithMessage("WaId must be a numeric E.164 phone number (no '+').");

        RuleFor(x => x.MessageId)
            .NotEmpty().WithMessage("MessageId is required.")
            .MaximumLength(512).WithMessage("MessageId must not exceed 512 characters.");

        RuleFor(x => x.MessageType)
            .NotEmpty().WithMessage("MessageType is required.")
            .MaximumLength(20).WithMessage("MessageType must not exceed 20 characters.");

        // TextBody is optional; only validated when present
        RuleFor(x => x.TextBody)
            .MaximumLength(4096).WithMessage("Message body must not exceed 4096 characters.")
            .When(x => x.TextBody is not null);

        RuleFor(x => x.LocationAddress)
            .MaximumLength(500).WithMessage("Location address must not exceed 500 characters.")
            .When(x => x.LocationAddress is not null);

        // GPS coordinates from WhatsApp location messages must be range-validated.
        // Without this check a malformed Meta webhook payload could inject impossible
        // coordinates (e.g. lat=9999) that get stored in BotSession and then forwarded
        // verbatim to the LostPetEvent, corrupting geocoding and proximity checks.
        RuleFor(x => x.LocationLat)
            .InclusiveBetween(-90.0, 90.0)
            .WithMessage("Latitude must be between -90 and 90.")
            .When(x => x.LocationLat.HasValue);

        RuleFor(x => x.LocationLng)
            .InclusiveBetween(-180.0, 180.0)
            .WithMessage("Longitude must be between -180 and 180.")
            .When(x => x.LocationLng.HasValue);
    }
}

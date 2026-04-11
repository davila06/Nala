using FluentValidation;

namespace PawTrack.Application.Locations.Commands.UpdateUserLocation;

public sealed class UpdateUserLocationCommandValidator : AbstractValidator<UpdateUserLocationCommand>
{
    public UpdateUserLocationCommandValidator()
    {
        RuleFor(x => x.Lat)
            .InclusiveBetween(-90.0, 90.0)
            .WithMessage("Latitude must be between -90 and 90.");

        RuleFor(x => x.Lng)
            .InclusiveBetween(-180.0, 180.0)
            .WithMessage("Longitude must be between -180 and 180.");

        // Quiet hours: either both are provided or neither is.
        RuleFor(x => x.QuietHoursEnd)
            .NotNull()
            .WithMessage("QuietHoursEnd is required when QuietHoursStart is set.")
            .When(x => x.QuietHoursStart is not null);

        RuleFor(x => x.QuietHoursStart)
            .NotNull()
            .WithMessage("QuietHoursStart is required when QuietHoursEnd is set.")
            .When(x => x.QuietHoursEnd is not null);

        RuleFor(x => x)
            .Must(x => x.QuietHoursStart != x.QuietHoursEnd)
            .WithMessage("QuietHoursStart and QuietHoursEnd must be different times.")
            .When(x => x.QuietHoursStart is not null && x.QuietHoursEnd is not null);
    }
}

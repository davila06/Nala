using FluentValidation;

namespace PawTrack.Application.Fosters.Commands.UpsertMyFosterProfile;

public sealed class UpsertMyFosterProfileCommandValidator : AbstractValidator<UpsertMyFosterProfileCommand>
{
    public UpsertMyFosterProfileCommandValidator()
    {
        RuleFor(x => x.HomeLat).InclusiveBetween(-90, 90);
        RuleFor(x => x.HomeLng).InclusiveBetween(-180, 180);

        RuleFor(x => x.AcceptedSpecies)
            .NotEmpty()
            .WithMessage("At least one accepted species is required.");

        RuleFor(x => x.MaxDays)
            .InclusiveBetween(1, 30);

        RuleFor(x => x.SizePreference)
            .MaximumLength(20);

        RuleFor(x => x.AvailableUntil)
            .Must((cmd, until) => until is null || until > DateTimeOffset.UtcNow || !cmd.IsAvailable)
            .WithMessage("AvailableUntil must be in the future when profile is available.");
    }
}

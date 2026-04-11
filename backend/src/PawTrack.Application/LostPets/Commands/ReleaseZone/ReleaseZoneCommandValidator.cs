using FluentValidation;

namespace PawTrack.Application.LostPets.Commands.ReleaseZone;

public sealed class ReleaseZoneCommandValidator : AbstractValidator<ReleaseZoneCommand>
{
    public ReleaseZoneCommandValidator()
    {
        RuleFor(x => x.ZoneId)
            .NotEmpty()
            .WithMessage("Zone ID must not be empty.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID must not be empty.");
    }
}

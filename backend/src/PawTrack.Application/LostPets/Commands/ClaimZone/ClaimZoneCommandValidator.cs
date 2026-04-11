using FluentValidation;

namespace PawTrack.Application.LostPets.Commands.ClaimZone;

public sealed class ClaimZoneCommandValidator : AbstractValidator<ClaimZoneCommand>
{
    public ClaimZoneCommandValidator()
    {
        RuleFor(x => x.ZoneId)
            .NotEmpty()
            .WithMessage("Zone ID must not be empty.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID must not be empty.");
    }
}

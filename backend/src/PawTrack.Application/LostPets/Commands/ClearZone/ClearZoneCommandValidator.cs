using FluentValidation;

namespace PawTrack.Application.LostPets.Commands.ClearZone;

public sealed class ClearZoneCommandValidator : AbstractValidator<ClearZoneCommand>
{
    public ClearZoneCommandValidator()
    {
        RuleFor(x => x.ZoneId)
            .NotEmpty()
            .WithMessage("Zone ID must not be empty.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID must not be empty.");
    }
}

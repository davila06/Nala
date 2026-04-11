using FluentValidation;

namespace PawTrack.Application.LostPets.Commands.ActivateSearchCoordination;

public sealed class ActivateSearchCoordinationCommandValidator
    : AbstractValidator<ActivateSearchCoordinationCommand>
{
    public ActivateSearchCoordinationCommandValidator()
    {
        RuleFor(x => x.LostPetEventId)
            .NotEmpty()
            .WithMessage("Lost pet event ID must not be empty.");

        RuleFor(x => x.RequestingUserId)
            .NotEmpty()
            .WithMessage("Requesting user ID must not be empty.");
    }
}

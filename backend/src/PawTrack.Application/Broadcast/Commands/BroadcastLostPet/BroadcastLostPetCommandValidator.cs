using FluentValidation;

namespace PawTrack.Application.Broadcast.Commands.BroadcastLostPet;

public sealed class BroadcastLostPetCommandValidator : AbstractValidator<BroadcastLostPetCommand>
{
    public BroadcastLostPetCommandValidator()
    {
        RuleFor(x => x.LostPetEventId)
            .NotEmpty().WithMessage("Lost pet event ID is required.");

        RuleFor(x => x.RequestingUserId)
            .NotEmpty().WithMessage("Requesting user ID is required.");
    }
}

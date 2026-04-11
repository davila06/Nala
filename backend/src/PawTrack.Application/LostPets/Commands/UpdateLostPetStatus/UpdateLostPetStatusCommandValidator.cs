using FluentValidation;

namespace PawTrack.Application.LostPets.Commands.UpdateLostPetStatus;

public sealed class UpdateLostPetStatusCommandValidator : AbstractValidator<UpdateLostPetStatusCommand>
{
    public UpdateLostPetStatusCommandValidator()
    {
        RuleFor(x => x.LostPetEventId)
            .NotEmpty();

        RuleFor(x => x.RequestingUserId)
            .NotEmpty();

        RuleFor(x => x.NewStatus)
            .IsInEnum();
    }
}

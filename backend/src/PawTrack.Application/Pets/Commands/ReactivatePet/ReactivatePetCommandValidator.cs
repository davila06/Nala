using FluentValidation;

namespace PawTrack.Application.Pets.Commands.ReactivatePet;

public sealed class ReactivatePetCommandValidator : AbstractValidator<ReactivatePetCommand>
{
    public ReactivatePetCommandValidator()
    {
        RuleFor(x => x.PetId)
            .NotEmpty()
            .WithMessage("Pet ID must not be empty.");

        RuleFor(x => x.RequestingUserId)
            .NotEmpty()
            .WithMessage("Requesting user ID must not be empty.");
    }
}

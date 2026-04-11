using FluentValidation;

namespace PawTrack.Application.Pets.Commands.DeletePet;

public sealed class DeletePetCommandValidator : AbstractValidator<DeletePetCommand>
{
    public DeletePetCommandValidator()
    {
        RuleFor(x => x.PetId)
            .NotEmpty()
            .WithMessage("Pet ID must not be empty.");

        RuleFor(x => x.RequestingUserId)
            .NotEmpty()
            .WithMessage("Requesting user ID must not be empty.");
    }
}

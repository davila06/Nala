using FluentValidation;

namespace PawTrack.Application.LostPets.Queries.GetActiveLostPetByPet;

public sealed class GetActiveLostPetByPetQueryValidator : AbstractValidator<GetActiveLostPetByPetQuery>
{
    public GetActiveLostPetByPetQueryValidator()
    {
        RuleFor(x => x.PetId)
            .NotEmpty()
            .WithMessage("Pet ID must not be empty.");

        RuleFor(x => x.RequestingUserId)
            .NotEmpty()
            .WithMessage("Requesting user ID must not be empty.");
    }
}

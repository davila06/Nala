using FluentValidation;

namespace PawTrack.Application.Sightings.Queries.GetSightingsByPet;

public sealed class GetSightingsByPetQueryValidator : AbstractValidator<GetSightingsByPetQuery>
{
    public GetSightingsByPetQueryValidator()
    {
        RuleFor(x => x.PetId)
            .NotEmpty()
            .WithMessage("Pet ID must not be empty.");

        RuleFor(x => x.RequestingUserId)
            .NotEmpty()
            .WithMessage("Requesting user ID must not be empty.");
    }
}

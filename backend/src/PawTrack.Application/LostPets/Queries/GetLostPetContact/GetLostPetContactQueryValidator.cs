using FluentValidation;

namespace PawTrack.Application.LostPets.Queries.GetLostPetContact;

public sealed class GetLostPetContactQueryValidator : AbstractValidator<GetLostPetContactQuery>
{
    public GetLostPetContactQueryValidator()
    {
        RuleFor(x => x.LostEventId)
            .NotEmpty()
            .WithMessage("Lost event ID must not be empty.");
    }
}

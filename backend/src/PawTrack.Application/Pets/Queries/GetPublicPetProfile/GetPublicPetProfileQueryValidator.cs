using FluentValidation;

namespace PawTrack.Application.Pets.Queries.GetPublicPetProfile;

public sealed class GetPublicPetProfileQueryValidator : AbstractValidator<GetPublicPetProfileQuery>
{
    public GetPublicPetProfileQueryValidator()
    {
        RuleFor(x => x.PetId)
            .NotEmpty()
            .WithMessage("Pet ID must not be empty.");
    }
}

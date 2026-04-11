using FluentValidation;

namespace PawTrack.Application.Pets.Queries.GetMyPets;

public sealed class GetMyPetsQueryValidator : AbstractValidator<GetMyPetsQuery>
{
    public GetMyPetsQueryValidator()
    {
        RuleFor(x => x.OwnerId)
            .NotEmpty()
            .WithMessage("Owner ID must not be empty.");
    }
}

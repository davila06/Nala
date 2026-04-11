using FluentValidation;

namespace PawTrack.Application.Pets.Queries.GetPetDetail;

public sealed class GetPetDetailQueryValidator : AbstractValidator<GetPetDetailQuery>
{
    public GetPetDetailQueryValidator()
    {
        RuleFor(x => x.PetId)
            .NotEmpty()
            .WithMessage("Pet ID must not be empty.");

        RuleFor(x => x.RequestingUserId)
            .NotEmpty()
            .WithMessage("Requesting user ID must not be empty.");
    }
}

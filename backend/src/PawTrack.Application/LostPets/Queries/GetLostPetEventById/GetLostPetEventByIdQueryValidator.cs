using FluentValidation;

namespace PawTrack.Application.LostPets.Queries.GetLostPetEventById;

public sealed class GetLostPetEventByIdQueryValidator : AbstractValidator<GetLostPetEventByIdQuery>
{
    public GetLostPetEventByIdQueryValidator()
    {
        RuleFor(x => x.LostPetEventId)
            .NotEmpty()
            .WithMessage("Lost pet event ID must not be empty.");

        RuleFor(x => x.RequestingUserId)
            .NotEmpty()
            .WithMessage("Requesting user ID must not be empty.");
    }
}

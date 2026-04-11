using FluentValidation;

namespace PawTrack.Application.Sightings.Queries.GetMovementPrediction;

public sealed class GetMovementPredictionQueryValidator : AbstractValidator<GetMovementPredictionQuery>
{
    public GetMovementPredictionQueryValidator()
    {
        RuleFor(x => x.LostPetEventId)
            .NotEmpty()
            .WithMessage("Lost pet event ID must not be empty.");
    }
}

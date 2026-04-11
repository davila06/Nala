using FluentValidation;

namespace PawTrack.Application.Sightings.VisualMatch;

public sealed class MatchSightingByIdQueryValidator : AbstractValidator<MatchSightingByIdQuery>
{
    public MatchSightingByIdQueryValidator()
    {
        RuleFor(x => x.SightingId)
            .NotEmpty()
            .WithMessage("Sighting ID must not be empty.");
    }
}

using FluentValidation;

namespace PawTrack.Application.Sightings.Queries.GetPublicMapEvents;

public sealed class GetPublicMapEventsQueryValidator : AbstractValidator<GetPublicMapEventsQuery>
{
    public GetPublicMapEventsQueryValidator()
    {
        RuleFor(x => x.North)
            .InclusiveBetween(-90, 90)
            .WithMessage("North latitude must be between -90 and 90.")
            .GreaterThan(x => x.South)
            .WithMessage("North latitude must be greater than South latitude.");

        RuleFor(x => x.South)
            .InclusiveBetween(-90, 90)
            .WithMessage("South latitude must be between -90 and 90.");

        RuleFor(x => x.East)
            .InclusiveBetween(-180, 180)
            .WithMessage("East longitude must be between -180 and 180.")
            .GreaterThan(x => x.West)
            .WithMessage("East longitude must be greater than West longitude.");

        RuleFor(x => x.West)
            .InclusiveBetween(-180, 180)
            .WithMessage("West longitude must be between -180 and 180.");
    }
}

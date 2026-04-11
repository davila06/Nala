using FluentValidation;

namespace PawTrack.Application.LostPets.Queries.GetSearchZones;

public sealed class GetSearchZonesQueryValidator : AbstractValidator<GetSearchZonesQuery>
{
    public GetSearchZonesQueryValidator()
    {
        RuleFor(x => x.LostPetEventId)
            .NotEmpty()
            .WithMessage("Lost pet event ID must not be empty.");
    }
}

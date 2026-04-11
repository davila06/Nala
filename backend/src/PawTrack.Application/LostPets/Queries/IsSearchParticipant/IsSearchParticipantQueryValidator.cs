using FluentValidation;

namespace PawTrack.Application.LostPets.Queries.IsSearchParticipant;

public sealed class IsSearchParticipantQueryValidator : AbstractValidator<IsSearchParticipantQuery>
{
    public IsSearchParticipantQueryValidator()
    {
        RuleFor(x => x.LostEventId)
            .NotEmpty()
            .WithMessage("Lost event ID must not be empty.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID must not be empty.");
    }
}

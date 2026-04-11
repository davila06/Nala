using FluentValidation;

namespace PawTrack.Application.LostPets.Queries.GetCaseRoom;

public sealed class GetCaseRoomQueryValidator : AbstractValidator<GetCaseRoomQuery>
{
    public GetCaseRoomQueryValidator()
    {
        RuleFor(x => x.LostPetEventId)
            .NotEmpty()
            .WithMessage("Lost pet event ID must not be empty.");

        RuleFor(x => x.RequestingUserId)
            .NotEmpty()
            .WithMessage("Requesting user ID must not be empty.");
    }
}

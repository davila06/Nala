using FluentValidation;

namespace PawTrack.Application.Broadcast.Queries.GetBroadcastStatus;

public sealed class GetBroadcastStatusQueryValidator : AbstractValidator<GetBroadcastStatusQuery>
{
    public GetBroadcastStatusQueryValidator()
    {
        RuleFor(x => x.LostPetEventId)
            .NotEmpty()
            .WithMessage("Lost pet event ID must not be empty.");

        RuleFor(x => x.RequestingUserId)
            .NotEmpty()
            .WithMessage("Requesting user ID must not be empty.");
    }
}

using FluentValidation;

namespace PawTrack.Application.Chat.Queries.GetChatThreads;

public sealed class GetChatThreadsQueryValidator : AbstractValidator<GetChatThreadsQuery>
{
    public GetChatThreadsQueryValidator()
    {
        RuleFor(x => x.LostPetEventId)
            .NotEmpty()
            .WithMessage("Lost pet event ID must not be empty.");

        RuleFor(x => x.RequestingUserId)
            .NotEmpty()
            .WithMessage("Requesting user ID must not be empty.");
    }
}

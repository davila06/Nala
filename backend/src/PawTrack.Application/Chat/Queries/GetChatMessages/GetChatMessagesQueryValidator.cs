using FluentValidation;

namespace PawTrack.Application.Chat.Queries.GetChatMessages;

public sealed class GetChatMessagesQueryValidator : AbstractValidator<GetChatMessagesQuery>
{
    public GetChatMessagesQueryValidator()
    {
        RuleFor(x => x.ThreadId)
            .NotEmpty()
            .WithMessage("Thread ID must not be empty.");

        RuleFor(x => x.RequestingUserId)
            .NotEmpty()
            .WithMessage("Requesting user ID must not be empty.");
    }
}

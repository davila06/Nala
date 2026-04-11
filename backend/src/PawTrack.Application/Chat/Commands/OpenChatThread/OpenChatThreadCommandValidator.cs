using FluentValidation;

namespace PawTrack.Application.Chat.Commands.OpenChatThread;

public sealed class OpenChatThreadCommandValidator : AbstractValidator<OpenChatThreadCommand>
{
    public OpenChatThreadCommandValidator()
    {
        RuleFor(x => x.LostPetEventId)
            .NotEmpty()
            .WithMessage("Lost pet event ID must not be empty.");

        RuleFor(x => x.InitiatorUserId)
            .NotEmpty()
            .WithMessage("Initiator user ID must not be empty.");
    }
}

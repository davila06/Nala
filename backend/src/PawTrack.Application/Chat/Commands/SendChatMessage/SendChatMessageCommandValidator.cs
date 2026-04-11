using FluentValidation;

namespace PawTrack.Application.Chat.Commands.SendChatMessage;

/// <summary>
/// Declarative pipeline-level validation for <see cref="SendChatMessageCommand"/>.
/// Mirrors the guards enforced in the handler so that the ValidationBehavior
/// catches invalid commands before they reach the handler.
/// </summary>
public sealed class SendChatMessageCommandValidator : AbstractValidator<SendChatMessageCommand>
{
    public SendChatMessageCommandValidator()
    {
        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("El mensaje no puede estar vacío.")
            .MaximumLength(Guards.MaxBodyLength)
            .WithMessage($"El mensaje no puede superar {Guards.MaxBodyLength} caracteres.");
    }
}

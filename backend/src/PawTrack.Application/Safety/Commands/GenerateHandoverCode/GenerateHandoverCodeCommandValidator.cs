using FluentValidation;

namespace PawTrack.Application.Safety.Commands.GenerateHandoverCode;

public sealed class GenerateHandoverCodeCommandValidator
    : AbstractValidator<GenerateHandoverCodeCommand>
{
    public GenerateHandoverCodeCommandValidator()
    {
        RuleFor(x => x.LostPetEventId)
            .NotEmpty()
            .WithMessage("Lost pet event ID must not be empty.");

        RuleFor(x => x.RequestingUserId)
            .NotEmpty()
            .WithMessage("Requesting user ID must not be empty.");
    }
}

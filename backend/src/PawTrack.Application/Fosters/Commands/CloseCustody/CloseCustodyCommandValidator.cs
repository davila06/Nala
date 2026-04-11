using FluentValidation;

namespace PawTrack.Application.Fosters.Commands.CloseCustody;

/// <summary>
/// Validates the close-custody request.
/// <c>Outcome</c> is persisted as a free-text label — needs an upper bound to
/// prevent DB column overflow and to ensure the frontend can always display it.
/// </summary>
public sealed class CloseCustodyCommandValidator : AbstractValidator<CloseCustodyCommand>
{
    private const int MaxOutcomeLength = 200;

    public CloseCustodyCommandValidator()
    {
        RuleFor(x => x.Outcome)
            .NotEmpty()
            .MaximumLength(MaxOutcomeLength)
            .WithMessage($"Outcome must not exceed {MaxOutcomeLength} characters.");
    }
}

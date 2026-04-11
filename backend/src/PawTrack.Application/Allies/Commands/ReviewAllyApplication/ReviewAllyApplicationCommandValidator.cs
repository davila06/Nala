using FluentValidation;

namespace PawTrack.Application.Allies.Commands.ReviewAllyApplication;

public sealed class ReviewAllyApplicationCommandValidator : AbstractValidator<ReviewAllyApplicationCommand>
{
    public ReviewAllyApplicationCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID must not be empty.");
    }
}

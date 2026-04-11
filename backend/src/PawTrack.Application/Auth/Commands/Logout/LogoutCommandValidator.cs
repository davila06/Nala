using FluentValidation;

namespace PawTrack.Application.Auth.Commands.Logout;

public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID must not be empty.");

        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token must not be empty.")
            .MaximumLength(2048)
            .WithMessage("Refresh token must not exceed 2048 characters.");

        // AccessTokenJti is a UUID string (max 36 chars); optional but bounded when present.
        RuleFor(x => x.AccessTokenJti)
            .MaximumLength(36)
            .WithMessage("Access token JTI must not exceed 36 characters.")
            .When(x => x.AccessTokenJti is not null);
    }
}

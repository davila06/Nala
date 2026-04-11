using FluentValidation;

namespace PawTrack.Application.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("Refresh token must not be empty.")
            .MaximumLength(2048)
            .WithMessage("Refresh token must not exceed 2048 characters.");
    }
}

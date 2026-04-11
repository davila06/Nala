using FluentValidation;

namespace PawTrack.Application.Auth.Commands.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            // Defence-in-depth: bcrypt truncates input at 72 bytes internally.
            // Rejecting longer inputs at the validation layer ensures the size-limit
            // check remains effective even if [RequestSizeLimit] is accidentally removed.
            .MaximumLength(72).WithMessage("Password must not exceed 72 characters.");
    }
}

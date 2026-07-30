using FluentValidation;

namespace PawTrack.Application.Auth.Commands.ChangePassword;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID must not be empty.");

        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("New password must be at least 8 characters.")
            .MaximumLength(128).WithMessage("New password must not exceed 128 characters.")
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("New password must differ from current password.");
    }
}

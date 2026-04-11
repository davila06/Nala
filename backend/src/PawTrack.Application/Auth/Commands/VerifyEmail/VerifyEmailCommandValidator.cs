using FluentValidation;

namespace PawTrack.Application.Auth.Commands.VerifyEmail;

public sealed class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Verification token is required.")
            // 32 raw bytes encoded as URL-safe base64 → 43 characters (trimmed '=').
            // We allow up to 64 to accommodate tokens from 32–48 source bytes with
            // no padding, giving a safe upper bound without rejecting future token
            // sizes if the generator is updated.
            .MaximumLength(64).WithMessage("Invalid verification token format.")
            // URL-safe base64 alphabet: A-Z, a-z, 0-9, '-', '_' (no '+', '/', '=').
            // Rejecting other characters prevents injection of %XX-encoded or special
            // chars into the SQL/log pipeline while preserving all valid tokens.
            .Matches(@"^[A-Za-z0-9_-]+$").WithMessage("Invalid verification token format.");
    }
}

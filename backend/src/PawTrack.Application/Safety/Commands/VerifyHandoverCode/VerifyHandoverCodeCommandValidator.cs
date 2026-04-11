using FluentValidation;

namespace PawTrack.Application.Safety.Commands.VerifyHandoverCode;

public sealed class VerifyHandoverCodeCommandValidator : AbstractValidator<VerifyHandoverCodeCommand>
{
    public VerifyHandoverCodeCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .Matches(@"^\d{4}$").WithMessage("Code must be exactly 4 digits.");
    }
}

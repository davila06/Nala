using FluentValidation;

namespace PawTrack.Application.Clinics.Commands.ReviewClinic;

public sealed class ReviewClinicCommandValidator : AbstractValidator<ReviewClinicCommand>
{
    public ReviewClinicCommandValidator()
    {
        RuleFor(x => x.ClinicId)
            .NotEmpty()
            .WithMessage("Clinic ID must not be empty.");
    }
}

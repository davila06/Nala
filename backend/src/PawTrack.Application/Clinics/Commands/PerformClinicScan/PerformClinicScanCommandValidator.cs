using FluentValidation;

namespace PawTrack.Application.Clinics.Commands.PerformClinicScan;

public sealed class PerformClinicScanCommandValidator : AbstractValidator<PerformClinicScanCommand>
{
    public PerformClinicScanCommandValidator()
    {
        RuleFor(x => x.Input)
            .NotEmpty().WithMessage("Scan input is required.")
            .MaximumLength(1024).WithMessage("Scan input must not exceed 1024 characters.");
    }
}

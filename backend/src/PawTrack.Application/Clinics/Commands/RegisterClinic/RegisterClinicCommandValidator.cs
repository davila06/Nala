using FluentValidation;

namespace PawTrack.Application.Clinics.Commands.RegisterClinic;

public sealed class RegisterClinicCommandValidator : AbstractValidator<RegisterClinicCommand>
{
    private static readonly HashSet<string> SpecialChars =
        [.. "!@#$%^&*()_+-=[]{}|;':\",./<>?\\`~".Select(c => c.ToString())];

    public RegisterClinicCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Clinic name is required.")
            .MaximumLength(200).WithMessage("Clinic name must not exceed 200 characters.");

        RuleFor(x => x.LicenseNumber)
            .NotEmpty().WithMessage("SENASA license number is required.")
            .MaximumLength(50).WithMessage("License number must not exceed 50 characters.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(300).WithMessage("Address must not exceed 300 characters.");

        RuleFor(x => x.Lat)
            .InclusiveBetween(-90m, 90m).WithMessage("Latitude must be between -90 and 90.");

        RuleFor(x => x.Lng)
            .InclusiveBetween(-180m, 180m).WithMessage("Longitude must be between -180 and 180.");

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("Contact email is required.")
            .MaximumLength(200).WithMessage("Email address must not exceed 200 characters.")
            .EmailAddress().WithMessage("Contact email is not a valid email address.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(72).WithMessage("Password must not exceed 72 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"\d").WithMessage("Password must contain at least one digit.")
            .Matches(@"[!@#$%^&*()_+\-=\[\]{};':"",./<>?\\`~]")
            .WithMessage("Password must contain at least one special character.");
    }
}

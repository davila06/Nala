using FluentValidation;
using PawTrack.Application.Common;

namespace PawTrack.Application.Sightings.Commands.ReportFoundPet;

public sealed class ReportFoundPetCommandValidator : AbstractValidator<ReportFoundPetCommand>
{
    private static readonly HashSet<string> AllowedMimeTypes =
        ["image/jpeg", "image/png", "image/webp"];

    private const long MaxPhotoBytes = 5 * 1024 * 1024; // 5 MB

    public ReportFoundPetCommandValidator()
    {
        RuleFor(x => x.FoundLat).InclusiveBetween(-90, 90);
        RuleFor(x => x.FoundLng).InclusiveBetween(-180, 180);
        RuleFor(x => x.ContactName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ContactPhone)
            .NotEmpty()
            .MaximumLength(30)
            .Matches(@"^[\d\s()\+\-\.]{7,30}$")
            .WithMessage("Contact phone contains invalid characters. Use digits, spaces, +, -, (, ) only.");
        RuleFor(x => x.BreedEstimate)
            .MaximumLength(100)
            .When(x => x.BreedEstimate is not null);
        RuleFor(x => x.ColorDescription)
            .MaximumLength(200)
            .When(x => x.ColorDescription is not null);
        RuleFor(x => x.SizeEstimate)
            .MaximumLength(50)
            .When(x => x.SizeEstimate is not null);
        RuleFor(x => x.Note)
            .MaximumLength(500)
            .When(x => x.Note is not null);

        // Photo — optional but, when provided, must pass MIME + size + magic-byte checks
        When(x => x.PhotoStream is not null, () =>
        {
            RuleFor(x => x.PhotoContentType)
                .NotEmpty().WithMessage("Photo content type is required when a photo is provided.")
                .Must(ct => AllowedMimeTypes.Contains(ct!.ToLowerInvariant()))
                .WithMessage("Photo must be JPEG, PNG, or WebP.");

            RuleFor(x => x.PhotoStream!)
                .Must(s => s.Length <= MaxPhotoBytes)
                .WithMessage("Photo must not exceed 5 MB.")
                .Must(s => ImageFileGuard.HasValidHeader(s))
                .WithMessage("Photo file format does not match the declared content type.");
        });
    }
}

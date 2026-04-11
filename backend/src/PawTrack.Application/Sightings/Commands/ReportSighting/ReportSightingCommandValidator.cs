using FluentValidation;
using PawTrack.Application.Common;

namespace PawTrack.Application.Sightings.Commands.ReportSighting;

public sealed class ReportSightingCommandValidator : AbstractValidator<ReportSightingCommand>
{
    private const long MaxPhotoBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly HashSet<string> AllowedMimeTypes =
        ["image/jpeg", "image/png", "image/webp"];

    public ReportSightingCommandValidator()
    {
        RuleFor(x => x.PetId)
            .NotEmpty().WithMessage("PetId is required.");

        RuleFor(x => x.Lat)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.");

        RuleFor(x => x.Lng)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.");

        RuleFor(x => x.RawNote)
            .MaximumLength(2000).WithMessage("Note must not exceed 2000 characters.")
            .When(x => x.RawNote is not null);

        RuleFor(x => x.SightedAt)
            .GreaterThanOrEqualTo(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero))
            .WithMessage("Sighting date is too far in the past to be valid.")
            .LessThanOrEqualTo(_ => DateTimeOffset.UtcNow.AddMinutes(5))
            .WithMessage("Sighting date cannot be in the future.");

        RuleFor(x => x.PhotoContentType)
            .Must(ct => ct is null || AllowedMimeTypes.Contains(ct))
            .WithMessage("Photo must be JPEG, PNG, or WebP.")
            .When(x => x.PhotoStream is not null);

        RuleFor(x => x.PhotoStream)
            .Must(s => s is null || s.Length <= MaxPhotoBytes)
            .WithMessage("Photo must not exceed 5 MB.")
            .Must(s => s is null || ImageFileGuard.HasValidHeader(s))
            .WithMessage("Photo file format does not match the declared content type.")
            .When(x => x.PhotoStream is not null);
    }
}

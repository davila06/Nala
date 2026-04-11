using FluentValidation;
using PawTrack.Application.Common;

namespace PawTrack.Application.LostPets.Commands.ReportLostPet;

public sealed class ReportLostPetCommandValidator : AbstractValidator<ReportLostPetCommand>
{
    private static readonly HashSet<string> AllowedMimeTypes =
        ["image/jpeg", "image/png", "image/webp"];

    private const long MaxPhotoBytes = 5 * 1024 * 1024; // 5 MB

    public ReportLostPetCommandValidator()
    {
        RuleFor(x => x.PetId)
            .NotEmpty().WithMessage("Pet ID is required.");

        RuleFor(x => x.RequestingUserId)
            .NotEmpty().WithMessage("Requesting user ID is required.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.PublicMessage)
            .MaximumLength(200).WithMessage("Public message must not exceed 200 characters.")
            .When(x => x.PublicMessage is not null);

        RuleFor(x => x.LastSeenAt)
            .LessThanOrEqualTo(DateTimeOffset.UtcNow.AddHours(1))
            .WithMessage("Last seen date cannot be in the future.");

        RuleFor(x => x.LastSeenLat)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.")
            .When(x => x.LastSeenLat.HasValue);

        RuleFor(x => x.LastSeenLng)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.")
            .When(x => x.LastSeenLng.HasValue);

        RuleFor(x => x.ContactName)
            .MaximumLength(100).WithMessage("Contact name must not exceed 100 characters.")
            .When(x => x.ContactName is not null);

        RuleFor(x => x.ContactPhone)
            .MaximumLength(30).WithMessage("Contact phone must not exceed 30 characters.")
            .Matches(@"^[\d\s()\+\-\.]{7,30}$")
            .WithMessage("Contact phone contains invalid characters. Use digits, spaces, +, -, (, ) only.")
            .When(x => x.ContactPhone is not null);

        RuleFor(x => x.RewardAmount)
            .GreaterThan(0).WithMessage("Reward amount must be greater than 0.")
            .LessThanOrEqualTo(10_000_000).WithMessage("Reward amount must not exceed 10,000,000 CRC.")
            .When(x => x.RewardAmount.HasValue);

        RuleFor(x => x.RewardNote)
            .MaximumLength(150).WithMessage("Reward note must not exceed 150 characters.")
            .When(x => x.RewardNote is not null);

        // Photo — optional but, when provided, must pass MIME + size + magic-byte checks
        When(x => x.PhotoBytes is not null, () =>
        {
            RuleFor(x => x.PhotoContentType)
                .NotEmpty().WithMessage("Photo content type is required when a photo is provided.")
                .Must(ct => AllowedMimeTypes.Contains(ct!.ToLowerInvariant()))
                .WithMessage("Photo must be JPEG, PNG, or WebP.");

            RuleFor(x => x.PhotoBytes!)
                .Must(b => b.Length <= MaxPhotoBytes)
                .WithMessage("Photo must not exceed 5 MB.")
                .Must(b => ImageFileGuard.HasValidHeader(b))
                .WithMessage("Photo file format does not match the declared content type.");
        });
    }
}

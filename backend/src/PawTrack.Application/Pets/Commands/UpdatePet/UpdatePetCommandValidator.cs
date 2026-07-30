using FluentValidation;
using PawTrack.Application.Common;

namespace PawTrack.Application.Pets.Commands.UpdatePet;

public sealed class UpdatePetCommandValidator : AbstractValidator<UpdatePetCommand>
{
    private static readonly HashSet<string> AllowedMimeTypes =
        ["image/jpeg", "image/png", "image/webp"];

    private const long MaxPhotoBytes = 5 * 1024 * 1024; // 5 MB

    public UpdatePetCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Pet name is required.")
            .MaximumLength(100).WithMessage("Pet name must not exceed 100 characters.");

        RuleFor(x => x.Species)
            .IsInEnum().WithMessage("Invalid species value.");

        RuleFor(x => x.Breed)
            .MaximumLength(100).WithMessage("Breed must not exceed 100 characters.")
            .When(x => x.Breed is not null);

        RuleFor(x => x.BirthDate)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Birth date cannot be in the future.")
            .When(x => x.BirthDate.HasValue);

        RuleFor(x => x.MicrochipId)
            .MaximumLength(15).WithMessage("Microchip ID must not exceed 15 characters (ISO 11784).")
            .Matches(@"^[A-F0-9]+$").WithMessage("Microchip ID must be hexadecimal (ISO 11784).")
            .When(x => x.MicrochipId is not null);

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

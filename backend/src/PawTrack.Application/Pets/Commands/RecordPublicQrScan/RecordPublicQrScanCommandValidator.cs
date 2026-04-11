using FluentValidation;

namespace PawTrack.Application.Pets.Commands.RecordPublicQrScan;

/// <summary>
/// Validates the public QR-scan telemetry command.
/// <para>
/// <b>Security (OWASP A03 – Injection / A04 – Insecure Design):</b>
/// <c>ScanLat</c> and <c>ScanLng</c> arrive from client-supplied query-string
/// parameters (<c>?scanLat=&amp;scanLng=</c>) on the public pet-profile endpoint.
/// Without range validation an attacker could inject impossible coordinate values
/// (e.g. <c>lat=9999</c>) that:
/// <list type="bullet">
///   <item>Falsely satisfy the proximity-to-owner-home proximity check, triggering
///   spurious "did you find your pet?" push notifications.</item>
///   <item>Persist as geographically nonsensical data in the <c>QrScanEvent</c> audit log.</item>
/// </list>
/// <c>UserAgent</c> and <c>CityName</c> are validated to match the DB column limits
/// enforced by <c>QrScanEventConfiguration</c> (512 and 100 chars respectively),
/// in addition to the controller-layer truncation already in place.
/// </para>
/// </summary>
public sealed class RecordPublicQrScanCommandValidator : AbstractValidator<RecordPublicQrScanCommand>
{
    public RecordPublicQrScanCommandValidator()
    {
        RuleFor(x => x.PetId)
            .NotEmpty().WithMessage("PetId is required.");

        // GPS coordinates are optional — not all clients support geolocation.
        // When present they must be within physical bounds; when one is provided
        // the other must also be present (a single coordinate is unusable).
        RuleFor(x => x.ScanLat)
            .InclusiveBetween(-90.0, 90.0)
            .WithMessage("Scan latitude must be between -90 and 90.")
            .When(x => x.ScanLat.HasValue);

        RuleFor(x => x.ScanLng)
            .InclusiveBetween(-180.0, 180.0)
            .WithMessage("Scan longitude must be between -180 and 180.")
            .When(x => x.ScanLng.HasValue);

        // If only one coordinate is provided the pair is meaningless.
        RuleFor(x => x.ScanLng)
            .NotNull()
            .WithMessage("ScanLng is required when ScanLat is provided.")
            .When(x => x.ScanLat.HasValue && !x.ScanLng.HasValue);

        RuleFor(x => x.ScanLat)
            .NotNull()
            .WithMessage("ScanLat is required when ScanLng is provided.")
            .When(x => x.ScanLng.HasValue && !x.ScanLat.HasValue);

        // Match QrScanEventConfiguration DB column limits.
        RuleFor(x => x.UserAgent)
            .MaximumLength(512)
            .WithMessage("UserAgent must not exceed 512 characters.")
            .When(x => x.UserAgent is not null);

        RuleFor(x => x.CityName)
            .MaximumLength(100)
            .WithMessage("CityName must not exceed 100 characters.")
            .When(x => x.CityName is not null);
    }
}

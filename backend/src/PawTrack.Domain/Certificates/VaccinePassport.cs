using PawTrack.Domain.Common;

namespace PawTrack.Domain.Certificates;

public sealed class VaccinePassport
{
    private readonly List<VaccinePassportVaccine> _vaccines = [];

    private VaccinePassport() { } // EF Core

    public Guid Id { get; private set; }
    public Guid CertificateId { get; private set; }
    public Guid PetId { get; private set; }
    public Guid IssuingClinicId { get; private set; }
    public Guid IssuingVeterinarianId { get; private set; }
    public string PetNameSnapshot { get; private set; } = string.Empty;
    public string PetSpeciesSnapshot { get; private set; } = string.Empty;
    public string? PetBreedSnapshot { get; private set; }
    public string? PetSexSnapshot { get; private set; }
    public string? PetColorSnapshot { get; private set; }
    public string? MicrochipSnapshot { get; private set; }
    public string? OwnerNameSnapshot { get; private set; }
    public string ClinicNameSnapshot { get; private set; } = string.Empty;
    public string ClinicLicenseSnapshot { get; private set; } = string.Empty;
    public string VetNameSnapshot { get; private set; } = string.Empty;
    public string VetLicenseSnapshot { get; private set; } = string.Empty;
    public DateTimeOffset IssuedAt { get; private set; }
    public DateOnly ValidUntil { get; private set; }
    public string VerificationCode { get; private set; } = string.Empty;
    public string FormatLabel { get; private set; } = "SENASA-ready / OIRSA-compatible";
    public string SchemaVersion { get; private set; } = "1.0";
    public VaccinePassportParasiteControl? ParasiteControl { get; private set; }

    public IReadOnlyList<VaccinePassportVaccine> Vaccines => _vaccines.AsReadOnly();

    public static VaccinePassport Issue(
        Guid certificateId,
        Guid petId,
        Guid issuingClinicId,
        Guid issuingVeterinarianId,
        VaccinePassportPetSnapshot petSnapshot,
        VaccinePassportIssuerSnapshot issuerSnapshot,
        IReadOnlyList<VaccinePassportVaccine> vaccines,
        VaccinePassportParasiteControl? parasiteControl,
        DateOnly validUntil,
        string verificationCode)
    {
        var result = TryIssue(
            certificateId,
            petId,
            issuingClinicId,
            issuingVeterinarianId,
            petSnapshot,
            issuerSnapshot,
            vaccines,
            parasiteControl,
            validUntil,
            verificationCode);

        if (result.IsFailure)
            throw new InvalidOperationException(string.Join(", ", result.Errors));

        return result.Value!;
    }

    public static Result<VaccinePassport> TryIssue(
        Guid certificateId,
        Guid petId,
        Guid issuingClinicId,
        Guid issuingVeterinarianId,
        VaccinePassportPetSnapshot petSnapshot,
        VaccinePassportIssuerSnapshot issuerSnapshot,
        IReadOnlyList<VaccinePassportVaccine> vaccines,
        VaccinePassportParasiteControl? parasiteControl,
        DateOnly validUntil,
        string verificationCode)
    {
        if (certificateId == Guid.Empty || petId == Guid.Empty || issuingClinicId == Guid.Empty || issuingVeterinarianId == Guid.Empty)
            return Result.Failure<VaccinePassport>("Los identificadores del pasaporte son requeridos.");

        if (string.IsNullOrWhiteSpace(verificationCode))
            return Result.Failure<VaccinePassport>("El código de verificación es requerido.");

        if (vaccines.Count == 0)
            return Result.Failure<VaccinePassport>("Al menos una vacuna es requerida para emitir el pasaporte.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (validUntil <= today)
            return Result.Failure<VaccinePassport>("La vigencia del pasaporte debe ser futura.");

        var passport = new VaccinePassport
        {
            Id = Guid.CreateVersion7(),
            CertificateId = certificateId,
            PetId = petId,
            IssuingClinicId = issuingClinicId,
            IssuingVeterinarianId = issuingVeterinarianId,
            PetNameSnapshot = petSnapshot.Name.Trim(),
            PetSpeciesSnapshot = petSnapshot.Species.Trim(),
            PetBreedSnapshot = NormalizeOptional(petSnapshot.Breed),
            PetSexSnapshot = NormalizeOptional(petSnapshot.Sex),
            PetColorSnapshot = NormalizeOptional(petSnapshot.Color),
            MicrochipSnapshot = NormalizeOptional(petSnapshot.Microchip)?.ToUpperInvariant(),
            OwnerNameSnapshot = NormalizeOptional(petSnapshot.OwnerName),
            ClinicNameSnapshot = issuerSnapshot.ClinicName.Trim(),
            ClinicLicenseSnapshot = issuerSnapshot.ClinicLicense.Trim().ToUpperInvariant(),
            VetNameSnapshot = issuerSnapshot.VetName.Trim(),
            VetLicenseSnapshot = issuerSnapshot.VetLicense.Trim().ToUpperInvariant(),
            IssuedAt = DateTimeOffset.UtcNow,
            ValidUntil = validUntil,
            VerificationCode = verificationCode.Trim().ToUpperInvariant(),
            ParasiteControl = parasiteControl,
        };

        passport._vaccines.AddRange(vaccines);
        return Result.Success(passport);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record VaccinePassportPetSnapshot(
    string Name,
    string Species,
    string? Breed,
    string? Sex,
    string? Color,
    string? Microchip,
    string? OwnerName);

public sealed record VaccinePassportIssuerSnapshot(
    string ClinicName,
    string ClinicLicense,
    string VetName,
    string VetLicense);

public sealed record VaccinePassportVaccine(
    string Name,
    string? Brand,
    string? LotNumber,
    DateOnly ApplicationDate,
    DateOnly? ValidUntil);

public sealed record VaccinePassportParasiteControl(
    string ProductName,
    DateOnly ApplicationDate,
    DateOnly? NextDueDate);

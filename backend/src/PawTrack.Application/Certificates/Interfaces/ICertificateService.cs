namespace PawTrack.Application.Certificates.Interfaces;

/// <summary>Generates a PDF certificate and stores it in blob storage, returning the public URL.</summary>
public interface ICertificateService
{
    Task<string> GenerateAndStoreAsync(
        CertificatePdfData data,
        CancellationToken cancellationToken = default);
}

public sealed record CertificatePdfData(
    string CertificateId,
    string VerificationCode,
    string PetName,
    string PetSpecies,
    string? PetBreed,
    string ClinicName,
    string ClinicLicense,
    string VetName,
    string CertificateType,
    string? Notes,
    DateTimeOffset IssuedAt,
    DateTimeOffset? ValidUntil,
    // ── Vaccine-passport-specific (OIRSA) ─────────────────────────────────
    string? OwnerName = null,
    string? MicrochipId = null,
    string? PetColor = null,
    IReadOnlyList<PassportVaccineEntry>? Vaccines = null,
    PassportParasiteEntry? ParasiteControl = null);

public sealed record PassportVaccineEntry(
    string VaccineName,
    string? Brand,
    string? LotNumber,
    DateOnly ApplicationDate,
    DateOnly? ValidUntil);

public sealed record PassportParasiteEntry(
    string ProductName,
    DateOnly ApplicationDate,
    DateOnly? NextDueDate);

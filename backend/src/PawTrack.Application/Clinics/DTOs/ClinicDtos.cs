using PawTrack.Domain.Clinics;

namespace PawTrack.Application.Clinics.DTOs;

public sealed record ClinicDto(
    Guid Id,
    string Name,
    string LicenseNumber,
    string Address,
    decimal Lat,
    decimal Lng,
    string ContactEmail,
    string Status,
    DateTimeOffset RegisteredAt)
{
    public static ClinicDto FromDomain(Clinic clinic) => new(
        clinic.Id,
        clinic.Name,
        clinic.LicenseNumber,
        clinic.Address,
        clinic.Lat,
        clinic.Lng,
        clinic.ContactEmail,
        clinic.Status.ToString(),
        clinic.RegisteredAt);
}

/// <summary>
/// Returned by the scan endpoint.
/// When <see cref="Matched"/> is false the owner/pet fields are all null.
/// </summary>
/// <remarks>
/// <b>Privacy:</b> <c>OwnerEmail</c> is intentionally absent.  The owner notification
/// is dispatched server-side via <c>DispatchClinicScanDetectedAsync</c>; the client
/// only needs to know whether the owner was contacted (<see cref="OwnerNotified"/>).
/// Returning the raw email would allow any active clinic account to harvest pet-owner
/// emails by systematically scanning QR codes.
/// </remarks>
public sealed record ClinicScanResultDto(
    Guid ScanId,
    bool Matched,
    string? PetName,
    string? PetPhotoUrl,
    string? OwnerName,
    bool OwnerNotified,
    string? PetSpecies);

using PawTrack.Domain.Certificates;

namespace PawTrack.Application.Certificates.DTOs;

public sealed record CertificateDto(
    Guid Id,
    Guid PetId,
    Guid ClinicId,
    CertificateType Type,
    string VerificationCode,
    string? PdfUrl,
    string? Notes,
    DateTimeOffset IssuedAt,
    DateTimeOffset? ValidUntil,
    bool IsRevoked,
    bool IsValid)
{
    public static CertificateDto FromDomain(VetCertificate c) => new(
        c.Id, c.PetId, c.ClinicId, c.Type,
        c.VerificationCode, c.PdfUrl, c.Notes,
        c.IssuedAt, c.ValidUntil, c.IsRevoked, c.IsValid);
}

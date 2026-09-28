using Microsoft.EntityFrameworkCore;
using PawTrack.Application.Medical;
using PawTrack.Infrastructure.Persistence;

namespace PawTrack.Infrastructure.Medical;

public sealed class HealthTimelineReadRepository(PawTrackDbContext db) : IHealthTimelineReadRepository
{
    public async Task<HealthTimelinePageDto> GetPageAsync(Guid petId, int offset, int pageSize, CancellationToken ct)
    {
        var limit = offset + pageSize + 1;
        var records = await db.MedicalRecords.AsNoTracking()
            .Where(record => record.PetId == petId && !record.IsSuperseded)
            .OrderByDescending(record => record.Date).ThenByDescending(record => record.Id)
            .Take(limit)
            .Select(record => new { record.Id, record.Date, record.Description, record.Type, record.DocumentUrl, record.DocumentKind })
            .ToListAsync(ct);
        var certificates = await db.VetCertificates.AsNoTracking()
            .Where(certificate => certificate.PetId == petId)
            .OrderByDescending(certificate => certificate.IssuedAt).ThenByDescending(certificate => certificate.Id)
            .Take(limit)
            .Select(certificate => new
            {
                certificate.Id,
                certificate.IssuedAt,
                certificate.Type,
                certificate.VerificationCode,
                certificate.IsRevoked,
                certificate.PdfUrl
            })
            .ToListAsync(ct);

        var items = records.Select(record => (
                Item: new HealthTimelineItemDto(record.Id, "MedicalRecord", record.Date,
                    record.Description, record.Type.ToString(), null, null, false,
                    record.DocumentKind?.ToString(), HasDocument: !string.IsNullOrWhiteSpace(record.DocumentUrl)),
                IssuedAt: DateTimeOffset.MinValue))
            .Concat(certificates.Select(certificate => (
                Item: new HealthTimelineItemDto(certificate.Id, "Certificate",
                    DateOnly.FromDateTime(certificate.IssuedAt.UtcDateTime), certificate.Type.ToString(),
                    "Certificate", null, certificate.VerificationCode, certificate.IsRevoked,
                    HasPdf: certificate.PdfUrl is not null),
                certificate.IssuedAt)))
            .OrderByDescending(row => row.Item.Date).ThenBy(row => row.Item.Source)
            .ThenByDescending(row => row.IssuedAt).ThenByDescending(row => row.Item.Id)
            .Skip(offset).Take(pageSize + 1).Select(row => row.Item).ToList();

        return new HealthTimelinePageDto(items.Take(pageSize).ToList(), items.Count > pageSize);
    }
}

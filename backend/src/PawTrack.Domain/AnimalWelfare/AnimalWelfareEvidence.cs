namespace PawTrack.Domain.AnimalWelfare;

public enum WelfareEvidenceKind
{
    Photo,
    Video,
    Document,
    VeterinaryNote,
    MunicipalReport,
}

public sealed class AnimalWelfareEvidence
{
    private AnimalWelfareEvidence() { } // EF Core

    public Guid Id { get; private set; }
    public Guid CaseId { get; private set; }
    public string BlobUrl { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public Guid? UploadedByUserId { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; }
    public WelfareEvidenceKind EvidenceKind { get; private set; }
    public bool IsSensitive { get; private set; }
    public string? HashSha256 { get; private set; }

    public static AnimalWelfareEvidence Create(
        Guid caseId,
        string blobUrl,
        string contentType,
        long fileSizeBytes,
        WelfareEvidenceKind evidenceKind,
        Guid? uploadedByUserId,
        bool isSensitive,
        string? hashSha256)
    {
        if (caseId == Guid.Empty) throw new ArgumentException("CaseId is required.", nameof(caseId));
        if (string.IsNullOrWhiteSpace(blobUrl)) throw new ArgumentException("Blob URL is required.", nameof(blobUrl));
        if (string.IsNullOrWhiteSpace(contentType)) throw new ArgumentException("ContentType is required.", nameof(contentType));
        if (fileSizeBytes <= 0) throw new ArgumentException("File size must be positive.", nameof(fileSizeBytes));

        return new AnimalWelfareEvidence
        {
            Id = Guid.CreateVersion7(),
            CaseId = caseId,
            BlobUrl = blobUrl.Trim(),
            ContentType = contentType.Trim().ToLowerInvariant(),
            FileSizeBytes = fileSizeBytes,
            UploadedByUserId = uploadedByUserId,
            EvidenceKind = evidenceKind,
            IsSensitive = isSensitive,
            HashSha256 = string.IsNullOrWhiteSpace(hashSha256) ? null : hashSha256.Trim(),
            UploadedAt = DateTimeOffset.UtcNow,
        };
    }
}

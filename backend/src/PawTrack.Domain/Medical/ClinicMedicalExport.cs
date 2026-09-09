namespace PawTrack.Domain.Medical;

public enum ClinicMedicalExportStatus
{
    Completed,
    Expired,
    Failed,
}

public sealed class ClinicMedicalExport
{
    private ClinicMedicalExport() { }
    public Guid Id { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid PetId { get; private set; }
    public Guid RequestedByUserId { get; private set; }
    public string BlobUrl { get; private set; } = string.Empty;
    public int RecordCount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public ClinicMedicalExportStatus Status { get; private set; }

    public static ClinicMedicalExport Complete(Guid clinicId, Guid petId, Guid userId, string blobUrl, int recordCount, TimeSpan lifetime) => new()
    {
        Id = Guid.CreateVersion7(),
        ClinicId = clinicId,
        PetId = petId,
        RequestedByUserId = userId,
        BlobUrl = blobUrl,
        RecordCount = recordCount,
        CreatedAt = DateTimeOffset.UtcNow,
        ExpiresAt = DateTimeOffset.UtcNow.Add(lifetime),
        Status = ClinicMedicalExportStatus.Completed,
    };

    public bool IsDownloadable => Status == ClinicMedicalExportStatus.Completed && ExpiresAt > DateTimeOffset.UtcNow;
    public void Expire() => Status = ClinicMedicalExportStatus.Expired;
}
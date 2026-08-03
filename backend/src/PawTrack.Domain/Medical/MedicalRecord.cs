namespace PawTrack.Domain.Medical;

public enum MedicalRecordType
{
    Vaccination,
    Deworming,
    VetVisit,
    Surgery,
    Other,
}

public sealed class MedicalRecord
{
    private MedicalRecord() { }

    public Guid Id { get; private set; }
    public Guid PetId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    /// <summary>Set when a Clinic account added this record; null for owner-created records.</summary>
    public Guid? ClinicId { get; private set; }
    public MedicalRecordType Type { get; private set; }
    public DateOnly Date { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? VetName { get; private set; }
    public string? ClinicName { get; private set; }
    public DateOnly? NextDueDate { get; private set; }
    public string? DocumentUrl { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static MedicalRecord Create(
        Guid petId,
        Guid createdByUserId,
        MedicalRecordType type,
        DateOnly date,
        string description,
        string? vetName,
        string? clinicName,
        DateOnly? nextDueDate,
        Guid? clinicId = null) => new()
        {
            Id = Guid.CreateVersion7(),
            PetId = petId,
            CreatedByUserId = createdByUserId,
            ClinicId = clinicId,
            Type = type,
            Date = date,
            Description = description.Trim(),
            VetName = vetName?.Trim(),
            ClinicName = clinicName?.Trim(),
            NextDueDate = nextDueDate,
            CreatedAt = DateTimeOffset.UtcNow,
        };

    public void SetDocumentUrl(string url) => DocumentUrl = url;

    public void Update(
        MedicalRecordType type,
        DateOnly date,
        string description,
        string? vetName,
        string? clinicName,
        DateOnly? nextDueDate)
    {
        Type = type;
        Date = date;
        Description = description.Trim();
        VetName = vetName?.Trim();
        ClinicName = clinicName?.Trim();
        NextDueDate = nextDueDate;
    }
}

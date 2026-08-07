namespace PawTrack.Domain.Medical;

public enum MedicalRecordType
{
    // stored as int — safe to rename; do NOT reorder or the DB values break
    Vaccine = 0,      // was Vaccination
    Deworming = 1,
    Checkup = 2,      // was VetVisit
    Surgery = 3,
    Other = 4,
    Medication = 5,
    Allergy = 6,
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

    // ── Per-visit health metrics ────────────────────────────────────────────
    /// <summary>Pet weight at this visit in kilograms.</summary>
    public decimal? WeightKg { get; private set; }

    // ── Structured medication fields (only meaningful when Type == Medication) ─
    public string? DosageDescription { get; private set; }
    public string? Frequency { get; private set; }
    public int? DurationDays { get; private set; }
    public DateOnly? MedicationEndDate { get; private set; }

    public static MedicalRecord Create(
        Guid petId,
        Guid createdByUserId,
        MedicalRecordType type,
        DateOnly date,
        string description,
        string? vetName,
        string? clinicName,
        DateOnly? nextDueDate,
        Guid? clinicId = null,
        decimal? weightKg = null,
        string? dosageDescription = null,
        string? frequency = null,
        int? durationDays = null,
        DateOnly? medicationEndDate = null) => new()
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
            WeightKg = weightKg,
            DosageDescription = dosageDescription?.Trim(),
            Frequency = frequency?.Trim(),
            DurationDays = durationDays,
            MedicationEndDate = medicationEndDate,
        };

    public void SetDocumentUrl(string url) => DocumentUrl = url;

    public void Update(
        MedicalRecordType type,
        DateOnly date,
        string description,
        string? vetName,
        string? clinicName,
        DateOnly? nextDueDate,
        decimal? weightKg = null,
        string? dosageDescription = null,
        string? frequency = null,
        int? durationDays = null,
        DateOnly? medicationEndDate = null)
    {
        Type = type;
        Date = date;
        Description = description.Trim();
        VetName = vetName?.Trim();
        ClinicName = clinicName?.Trim();
        NextDueDate = nextDueDate;
        WeightKg = weightKg;
        DosageDescription = dosageDescription?.Trim();
        Frequency = frequency?.Trim();
        DurationDays = durationDays;
        MedicationEndDate = medicationEndDate;
    }
}

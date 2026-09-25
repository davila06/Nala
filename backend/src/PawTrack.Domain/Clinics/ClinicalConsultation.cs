namespace PawTrack.Domain.Clinics;

public enum ClinicalConsultationStatus
{
    Draft,
    Closed,
}

public sealed class ClinicalConsultation
{
    private ClinicalConsultation() { }

    public Guid Id { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid AppointmentId { get; private set; }
    public Guid PetId { get; private set; }
    public Guid VeterinarianId { get; private set; }
    public Guid OwnerId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public ClinicalConsultationStatus Status { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string Subjective { get; private set; } = string.Empty;
    public string Objective { get; private set; } = string.Empty;
    public string Assessment { get; private set; } = string.Empty;
    public string Plan { get; private set; } = string.Empty;
    public decimal? WeightKg { get; private set; }
    public decimal? TemperatureC { get; private set; }
    public int? HeartRateBpm { get; private set; }
    public int? RespiratoryRateRpm { get; private set; }
    public int? BodyConditionScore { get; private set; }
    public int? PainScore { get; private set; }
    public string? HydrationStatus { get; private set; }
    public string Diagnosis { get; private set; } = string.Empty;
    public string Treatment { get; private set; } = string.Empty;
    public string OwnerSummary { get; private set; } = string.Empty;
    public string? PrescriptionInstructions { get; private set; }
    public string? AttachmentUrl { get; private set; }
    public Guid? ClosedByUserId { get; private set; }
    public string? SignedByName { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static ClinicalConsultation Create(
        Guid clinicId,
        Guid appointmentId,
        Guid petId,
        Guid veterinarianId,
        Guid ownerId,
        Guid createdByUserId,
        string reason,
        string subjective,
        string objective,
        string assessment,
        string plan,
        decimal? weightKg,
        decimal? temperatureC,
        int? heartRateBpm,
        int? respiratoryRateRpm,
        int? bodyConditionScore,
        int? painScore,
        string? hydrationStatus,
        string diagnosis,
        string treatment,
        string ownerSummary)
    {
        if (clinicId == Guid.Empty) throw new ArgumentException("ClinicId is required.", nameof(clinicId));
        if (appointmentId == Guid.Empty) throw new ArgumentException("AppointmentId is required.", nameof(appointmentId));
        if (petId == Guid.Empty) throw new ArgumentException("PetId is required.", nameof(petId));
        if (veterinarianId == Guid.Empty) throw new ArgumentException("VeterinarianId is required.", nameof(veterinarianId));
        if (ownerId == Guid.Empty) throw new ArgumentException("OwnerId is required.", nameof(ownerId));
        if (createdByUserId == Guid.Empty) throw new ArgumentException("CreatedByUserId is required.", nameof(createdByUserId));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Reason is required.", nameof(reason));
        if (string.IsNullOrWhiteSpace(ownerSummary)) throw new ArgumentException("Owner summary is required.", nameof(ownerSummary));
        ValidateVitals(weightKg, temperatureC, heartRateBpm, respiratoryRateRpm, bodyConditionScore, painScore);

        return new ClinicalConsultation
        {
            Id = Guid.CreateVersion7(),
            ClinicId = clinicId,
            AppointmentId = appointmentId,
            PetId = petId,
            VeterinarianId = veterinarianId,
            OwnerId = ownerId,
            CreatedByUserId = createdByUserId,
            Status = ClinicalConsultationStatus.Draft,
            Reason = reason.Trim(),
            Subjective = subjective.Trim(),
            Objective = objective.Trim(),
            Assessment = assessment.Trim(),
            Plan = plan.Trim(),
            WeightKg = weightKg,
            TemperatureC = temperatureC,
            HeartRateBpm = heartRateBpm,
            RespiratoryRateRpm = respiratoryRateRpm,
            BodyConditionScore = bodyConditionScore,
            PainScore = painScore,
            HydrationStatus = string.IsNullOrWhiteSpace(hydrationStatus) ? null : hydrationStatus.Trim(),
            Diagnosis = diagnosis.Trim(),
            Treatment = treatment.Trim(),
            OwnerSummary = ownerSummary.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void SetAttachmentUrl(string url)
    {
        if (Status == ClinicalConsultationStatus.Closed)
            throw new InvalidOperationException("Closed consultations cannot be modified.");
        AttachmentUrl = string.IsNullOrWhiteSpace(url) ? null : url.Trim();
    }

    public void SetPrescription(string? prescriptionInstructions)
    {
        if (Status == ClinicalConsultationStatus.Closed)
            throw new InvalidOperationException("Closed consultations cannot be modified.");
        PrescriptionInstructions = string.IsNullOrWhiteSpace(prescriptionInstructions) ? null : prescriptionInstructions.Trim();
    }

    public void Close(Guid closedByUserId, string signedByName)
    {
        if (Status != ClinicalConsultationStatus.Draft)
            throw new InvalidOperationException("Only draft consultations can be closed.");
        if (closedByUserId == Guid.Empty) throw new ArgumentException("ClosedByUserId is required.", nameof(closedByUserId));
        if (string.IsNullOrWhiteSpace(signedByName)) throw new ArgumentException("Signature is required.", nameof(signedByName));

        Status = ClinicalConsultationStatus.Closed;
        ClosedByUserId = closedByUserId;
        SignedByName = signedByName.Trim();
        ClosedAt = DateTimeOffset.UtcNow;
    }

    private static void ValidateVitals(
        decimal? weightKg,
        decimal? temperatureC,
        int? heartRateBpm,
        int? respiratoryRateRpm,
        int? bodyConditionScore,
        int? painScore)
    {
        if (weightKg is <= 0 or > 250) throw new ArgumentOutOfRangeException(nameof(weightKg));
        if (temperatureC is < 30 or > 45) throw new ArgumentOutOfRangeException(nameof(temperatureC));
        if (heartRateBpm is <= 0 or > 400) throw new ArgumentOutOfRangeException(nameof(heartRateBpm));
        if (respiratoryRateRpm is <= 0 or > 200) throw new ArgumentOutOfRangeException(nameof(respiratoryRateRpm));
        if (bodyConditionScore is < 1 or > 9) throw new ArgumentOutOfRangeException(nameof(bodyConditionScore));
        if (painScore is < 0 or > 10) throw new ArgumentOutOfRangeException(nameof(painScore));
    }
}

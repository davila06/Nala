namespace PawTrack.Domain.CastrationCampaigns;

public sealed class CastrationAppointment
{
    private CastrationAppointment() { }

    public Guid Id { get; private set; }
    public Guid CampaignId { get; private set; }
    public Guid PetId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public DateTimeOffset ScheduledAt { get; private set; }
    public string EligibilitySnapshot { get; private set; } = string.Empty;
    public string ConsentVersion { get; private set; } = string.Empty;
    public DateTimeOffset ConsentAcceptedAt { get; private set; }
    public decimal BasePriceCrc { get; private set; }
    public decimal IvaAmountCrc { get; private set; }
    public decimal TotalAmountCrc { get; private set; }
    public CastrationAppointmentStatus Status { get; private set; }
    public Guid? ExecutingVeterinarianId { get; private set; }
    public string? ClinicalOutcome { get; private set; }
    public string? PostOperativeInstructions { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static CastrationAppointment Create(
        Guid campaignId,
        Guid petId,
        Guid ownerUserId,
        DateTimeOffset scheduledAt,
        string eligibilitySnapshot,
        string consentVersion,
        DateTimeOffset consentAcceptedAt,
        decimal basePriceCrc,
        decimal ivaAmountCrc,
        decimal totalAmountCrc)
    {
        if (campaignId == Guid.Empty) throw new ArgumentException("Campaign is required.", nameof(campaignId));
        if (petId == Guid.Empty) throw new ArgumentException("Pet is required.", nameof(petId));
        if (ownerUserId == Guid.Empty) throw new ArgumentException("Owner is required.", nameof(ownerUserId));
        ArgumentException.ThrowIfNullOrWhiteSpace(eligibilitySnapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(consentVersion);
        if (consentAcceptedAt == default) throw new ArgumentException("Consent acceptance is required.", nameof(consentAcceptedAt));
        if (basePriceCrc < 0 || ivaAmountCrc < 0 || totalAmountCrc < 0)
            throw new ArgumentOutOfRangeException(nameof(totalAmountCrc), "Amounts cannot be negative.");
        if (basePriceCrc + ivaAmountCrc != totalAmountCrc)
            throw new ArgumentException("Total amount must equal base price plus IVA.", nameof(totalAmountCrc));

        return new CastrationAppointment
        {
            Id = Guid.CreateVersion7(),
            CampaignId = campaignId,
            PetId = petId,
            OwnerUserId = ownerUserId,
            ScheduledAt = scheduledAt,
            EligibilitySnapshot = eligibilitySnapshot,
            ConsentVersion = consentVersion,
            ConsentAcceptedAt = consentAcceptedAt,
            BasePriceCrc = basePriceCrc,
            IvaAmountCrc = ivaAmountCrc,
            TotalAmountCrc = totalAmountCrc,
            Status = CastrationAppointmentStatus.Reserved,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Confirm() => SetStatus(
        CastrationAppointmentStatus.Reserved,
        CastrationAppointmentStatus.Confirmed,
        "Only reserved appointments can be confirmed.");

    public void CheckIn() => SetStatus(
        CastrationAppointmentStatus.Confirmed,
        CastrationAppointmentStatus.CheckedIn,
        "Only confirmed appointments can be checked in.");

    public void Complete(Guid veterinarianId, string clinicalOutcome, string postOperativeInstructions)
    {
        if (Status != CastrationAppointmentStatus.CheckedIn)
            throw new InvalidOperationException("Only checked-in appointments can be completed.");
        if (veterinarianId == Guid.Empty) throw new ArgumentException("Veterinarian is required.", nameof(veterinarianId));
        ArgumentException.ThrowIfNullOrWhiteSpace(clinicalOutcome);
        ArgumentException.ThrowIfNullOrWhiteSpace(postOperativeInstructions);

        ExecutingVeterinarianId = veterinarianId;
        ClinicalOutcome = clinicalOutcome.Trim();
        PostOperativeInstructions = postOperativeInstructions.Trim();
        Status = CastrationAppointmentStatus.Completed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkNoShow() => SetStatus(
        CastrationAppointmentStatus.Confirmed,
        CastrationAppointmentStatus.NoShow,
        "Only confirmed appointments can be marked as no-show.");

    public void Cancel()
    {
        if (Status is not (CastrationAppointmentStatus.Reserved or CastrationAppointmentStatus.Confirmed))
            throw new InvalidOperationException("Only reserved or confirmed appointments can be cancelled.");
        Status = CastrationAppointmentStatus.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void SetStatus(
        CastrationAppointmentStatus expected,
        CastrationAppointmentStatus next,
        string message)
    {
        if (Status != expected) throw new InvalidOperationException(message);
        Status = next;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

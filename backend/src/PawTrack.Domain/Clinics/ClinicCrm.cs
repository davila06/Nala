namespace PawTrack.Domain.Clinics;

public enum ClinicCommunicationChannel
{
    WhatsApp,
    Email,
    Phone,
    InApp,
}

public enum ClinicCommunicationPurpose
{
    AppointmentConfirmation,
    ClinicalFollowUp,
    VaccineReminder,
    PrescriptionDelivery,
    Billing,
    Marketing,
}

public enum ClinicCommunicationDirection
{
    Outbound,
    Inbound,
}

public enum ClinicCommunicationStatus
{
    Draft,
    Queued,
    Sent,
    Delivered,
    Failed,
    LoggedExternally,
}

public enum ClinicCrmTaskType
{
    CallClient,
    ConfirmAppointment,
    FollowUpTreatment,
    SendDocument,
    CollectPayment,
    Reactivation,
}

public enum ClinicCrmTaskStatus
{
    Open,
    Completed,
    Cancelled,
}

public sealed class ClinicClientCommunicationPreference
{
    private ClinicClientCommunicationPreference() { }

    public Guid Id { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid PetId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public ClinicCommunicationChannel Channel { get; private set; }
    public ClinicCommunicationPurpose Purpose { get; private set; }
    public bool IsOptedIn { get; private set; }
    public string ConsentSource { get; private set; } = string.Empty;
    public Guid UpdatedByUserId { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static ClinicClientCommunicationPreference Create(
        Guid clinicId,
        Guid petId,
        Guid ownerUserId,
        ClinicCommunicationChannel channel,
        ClinicCommunicationPurpose purpose,
        bool isOptedIn,
        string consentSource,
        Guid updatedByUserId)
    {
        if (clinicId == Guid.Empty) throw new ArgumentException("ClinicId is required.", nameof(clinicId));
        if (petId == Guid.Empty) throw new ArgumentException("PetId is required.", nameof(petId));
        if (ownerUserId == Guid.Empty) throw new ArgumentException("OwnerUserId is required.", nameof(ownerUserId));
        var preference = new ClinicClientCommunicationPreference
        {
            Id = Guid.CreateVersion7(),
            ClinicId = clinicId,
            PetId = petId,
            OwnerUserId = ownerUserId,
            Channel = channel,
            Purpose = purpose,
        };
        preference.Update(isOptedIn, consentSource, updatedByUserId);
        return preference;
    }

    public void Update(bool isOptedIn, string consentSource, Guid updatedByUserId)
    {
        if (string.IsNullOrWhiteSpace(consentSource)) throw new ArgumentException("Consent source is required.", nameof(consentSource));
        if (updatedByUserId == Guid.Empty) throw new ArgumentException("UpdatedByUserId is required.", nameof(updatedByUserId));
        IsOptedIn = isOptedIn;
        ConsentSource = consentSource.Trim();
        UpdatedByUserId = updatedByUserId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class ClinicClientCommunicationActivity
{
    private ClinicClientCommunicationActivity() { }

    public Guid Id { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid PetId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public ClinicCommunicationChannel Channel { get; private set; }
    public ClinicCommunicationPurpose Purpose { get; private set; }
    public ClinicCommunicationDirection Direction { get; private set; }
    public ClinicCommunicationStatus Status { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string? ProviderMessageId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? RequestId { get; private set; }

    public static ClinicClientCommunicationActivity Log(
        Guid clinicId,
        Guid petId,
        Guid ownerUserId,
        ClinicCommunicationChannel channel,
        ClinicCommunicationPurpose purpose,
        ClinicCommunicationDirection direction,
        ClinicCommunicationStatus status,
        string subject,
        string body,
        string? providerMessageId,
        Guid createdByUserId,
        Guid? requestId = null)
    {
        if (clinicId == Guid.Empty) throw new ArgumentException("ClinicId is required.", nameof(clinicId));
        if (petId == Guid.Empty) throw new ArgumentException("PetId is required.", nameof(petId));
        if (ownerUserId == Guid.Empty) throw new ArgumentException("OwnerUserId is required.", nameof(ownerUserId));
        if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Subject is required.", nameof(subject));
        if (string.IsNullOrWhiteSpace(body)) throw new ArgumentException("Body is required.", nameof(body));
        return new ClinicClientCommunicationActivity
        {
            Id = Guid.CreateVersion7(),
            ClinicId = clinicId,
            PetId = petId,
            OwnerUserId = ownerUserId,
            Channel = channel,
            Purpose = purpose,
            Direction = direction,
            Status = status,
            Subject = subject.Trim(),
            Body = body.Trim(),
            ProviderMessageId = string.IsNullOrWhiteSpace(providerMessageId) ? null : providerMessageId.Trim(),
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            RequestId = requestId,
        };
    }

    public void MarkProviderAccepted(string providerMessageId)
    {
        if (Status != ClinicCommunicationStatus.Queued) throw new InvalidOperationException("Activity is not queued.");
        if (string.IsNullOrWhiteSpace(providerMessageId)) throw new ArgumentException("Provider receipt required.", nameof(providerMessageId));
        ProviderMessageId = providerMessageId.Trim();
        Status = ClinicCommunicationStatus.Sent;
    }

    public void MarkProviderFailed()
    {
        if (Status != ClinicCommunicationStatus.Queued) throw new InvalidOperationException("Activity is not queued.");
        Status = ClinicCommunicationStatus.Failed;
    }
}

public sealed class ClinicCrmTask
{
    private ClinicCrmTask() { }

    public Guid Id { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid PetId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public ClinicCrmTaskType Type { get; private set; }
    public ClinicCrmTaskStatus Status { get; private set; }
    public DateOnly DueDate { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CompletedByUserId { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public static ClinicCrmTask Create(
        Guid clinicId,
        Guid petId,
        Guid ownerUserId,
        ClinicCrmTaskType type,
        DateOnly dueDate,
        string title,
        string? notes,
        Guid createdByUserId)
    {
        if (clinicId == Guid.Empty) throw new ArgumentException("ClinicId is required.", nameof(clinicId));
        if (petId == Guid.Empty) throw new ArgumentException("PetId is required.", nameof(petId));
        if (ownerUserId == Guid.Empty) throw new ArgumentException("OwnerUserId is required.", nameof(ownerUserId));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));
        return new ClinicCrmTask
        {
            Id = Guid.CreateVersion7(),
            ClinicId = clinicId,
            PetId = petId,
            OwnerUserId = ownerUserId,
            Type = type,
            Status = ClinicCrmTaskStatus.Open,
            DueDate = dueDate,
            Title = title.Trim(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Complete(Guid completedByUserId)
    {
        if (Status != ClinicCrmTaskStatus.Open)
            throw new InvalidOperationException("Only open CRM tasks can be completed.");
        if (completedByUserId == Guid.Empty)
            throw new ArgumentException("CompletedByUserId is required.", nameof(completedByUserId));
        Status = ClinicCrmTaskStatus.Completed;
        CompletedByUserId = completedByUserId;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}

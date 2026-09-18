namespace PawTrack.Domain.CastrationCampaigns;

public sealed class CastrationCampaign
{
    private CastrationCampaign() { }

    public Guid Id { get; private set; }
    public Guid OrganizerUserId { get; private set; }
    public Guid ExecutingClinicId { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string VenueLabel { get; private set; } = string.Empty;
    public string Canton { get; private set; } = string.Empty;
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public DateTimeOffset ReservationsOpenAt { get; private set; }
    public DateTimeOffset ReservationsCloseAt { get; private set; }
    public int Capacity { get; private set; }
    public int ReservedCount { get; private set; }
    public decimal BasePriceCrc { get; private set; }
    public string ConsentVersion { get; private set; } = string.Empty;
    public CastrationCampaignStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public int AvailableCapacity => Capacity - ReservedCount;

    public static CastrationCampaign Create(
        Guid organizerUserId,
        Guid executingClinicId,
        string title,
        string venueLabel,
        string canton,
        double latitude,
        double longitude,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        DateTimeOffset reservationsOpenAt,
        DateTimeOffset reservationsCloseAt,
        int capacity,
        decimal basePriceCrc,
        string consentVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(venueLabel);
        ArgumentException.ThrowIfNullOrWhiteSpace(canton);
        ArgumentException.ThrowIfNullOrWhiteSpace(consentVersion);

        if (organizerUserId == Guid.Empty) throw new ArgumentException("Organizer is required.", nameof(organizerUserId));
        if (executingClinicId == Guid.Empty) throw new ArgumentException("Executing clinic is required.", nameof(executingClinicId));
        if (latitude is < -90 or > 90) throw new ArgumentOutOfRangeException(nameof(latitude));
        if (longitude is < -180 or > 180) throw new ArgumentOutOfRangeException(nameof(longitude));
        if (endsAt <= startsAt) throw new ArgumentException("Campaign end must be after its start.", nameof(endsAt));
        if (reservationsCloseAt <= reservationsOpenAt) throw new ArgumentException("Reservation close must be after opening.", nameof(reservationsCloseAt));
        if (reservationsCloseAt > startsAt) throw new ArgumentException("Reservations must close before the campaign starts.", nameof(reservationsCloseAt));
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        if (basePriceCrc < 0) throw new ArgumentOutOfRangeException(nameof(basePriceCrc));

        return new CastrationCampaign
        {
            Id = Guid.CreateVersion7(),
            OrganizerUserId = organizerUserId,
            ExecutingClinicId = executingClinicId,
            Title = title.Trim(),
            VenueLabel = venueLabel.Trim(),
            Canton = canton.Trim(),
            Latitude = latitude,
            Longitude = longitude,
            StartsAt = startsAt,
            EndsAt = endsAt,
            ReservationsOpenAt = reservationsOpenAt,
            ReservationsCloseAt = reservationsCloseAt,
            Capacity = capacity,
            BasePriceCrc = basePriceCrc,
            ConsentVersion = consentVersion.Trim(),
            Status = CastrationCampaignStatus.Draft,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void SubmitForApproval()
    {
        EnsureStatus(CastrationCampaignStatus.Draft, "Only draft campaigns can be submitted for approval.");
        SetStatus(CastrationCampaignStatus.PendingApproval);
    }

    public void Approve(Guid approvedByUserId)
    {
        EnsureStatus(CastrationCampaignStatus.PendingApproval, "Only pending campaigns can be approved.");
        if (approvedByUserId == Guid.Empty) throw new ArgumentException("Approver is required.", nameof(approvedByUserId));
        ApprovedByUserId = approvedByUserId;
        SetStatus(CastrationCampaignStatus.Approved);
    }

    public void Publish()
    {
        EnsureStatus(CastrationCampaignStatus.Approved, "Campaign must be approved before publication.");
        SetStatus(CastrationCampaignStatus.Published);
    }

    public void ReserveSlot()
    {
        if (Status != CastrationCampaignStatus.Published)
            throw new InvalidOperationException("Campaign is not accepting reservations.");
        if (ReservedCount >= Capacity)
            throw new InvalidOperationException("Campaign capacity is exhausted.");

        ReservedCount++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ReleaseSlot()
    {
        if (ReservedCount <= 0) throw new InvalidOperationException("Campaign has no reserved slots to release.");
        ReservedCount--;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void EnsureStatus(CastrationCampaignStatus expected, string message)
    {
        if (Status != expected) throw new InvalidOperationException(message);
    }

    private void SetStatus(CastrationCampaignStatus status)
    {
        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

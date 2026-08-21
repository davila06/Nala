namespace PawTrack.Domain.Adoptions;

public enum FairStatus { Upcoming, Active, Finished, Cancelled }

public sealed class AdoptionFair
{
    private AdoptionFair() { } // EF Core
    private readonly List<Guid> _animalIds = [];

    public Guid Id { get; private set; }
    public Guid OrganizationUserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string VenueLabel { get; private set; } = string.Empty;
    public double Lat { get; private set; }
    public double Lng { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public FairStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    /// <summary>IDs of AdoptablePet records present at this fair.</summary>
    public IReadOnlyList<Guid> AnimalIds => _animalIds.AsReadOnly();

    public bool IsCurrentlyActive =>
        Status == FairStatus.Active &&
        DateTimeOffset.UtcNow >= StartsAt &&
        DateTimeOffset.UtcNow < EndsAt;

    // ── Factory ───────────────────────────────────────────────────────────────

    public static AdoptionFair Create(
        Guid organizationUserId,
        string title,
        string venueLabel,
        double lat,
        double lng,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        string? description = null)
    {
        if (endsAt <= startsAt)
            throw new ArgumentException("EndsAt must be after StartsAt.");

        return new AdoptionFair
        {
            Id = Guid.CreateVersion7(),
            OrganizationUserId = organizationUserId,
            Title = title.Trim(),
            Description = description?.Trim(),
            VenueLabel = venueLabel.Trim(),
            Lat = lat,
            Lng = lng,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Status = FairStatus.Upcoming,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    // ── Behaviour ─────────────────────────────────────────────────────────────

    public void AddAnimal(Guid animalId)
    {
        if (!_animalIds.Contains(animalId))
            _animalIds.Add(animalId);
    }

    public void RemoveAnimal(Guid animalId) => _animalIds.Remove(animalId);

    public void Activate()  { Status = FairStatus.Active;    UpdatedAt = DateTimeOffset.UtcNow; }
    public void Finish()    { Status = FairStatus.Finished;  UpdatedAt = DateTimeOffset.UtcNow; }
    public void Cancel()    { Status = FairStatus.Cancelled; UpdatedAt = DateTimeOffset.UtcNow; }
}

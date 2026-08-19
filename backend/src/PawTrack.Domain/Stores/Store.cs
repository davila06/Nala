namespace PawTrack.Domain.Stores;

public sealed class Store
{
    private Store() { }

    public Guid Id { get; private set; }
    /// <summary>FK to Auth.Users — the store owner account (Role = Store).</summary>
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public decimal Lat { get; private set; }
    public decimal Lng { get; private set; }
    public string ContactEmail { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public string? Website { get; private set; }
    public string? LogoUrl { get; private set; }
    /// <summary>True when the store has an active StorePlus or StorePartner subscription.</summary>
    public bool IsFeatured { get; private set; }
    public StoreStatus Status { get; private set; }
    public DateTimeOffset RegisteredAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Store Create(
        Guid userId,
        string name,
        string description,
        string address,
        decimal lat,
        decimal lng,
        string contactEmail) => new()
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Name = name.Trim(),
            Description = description.Trim(),
            Address = address.Trim(),
            Lat = lat,
            Lng = lng,
            ContactEmail = contactEmail.Trim().ToLowerInvariant(),
            Status = StoreStatus.Pending,
            RegisteredAt = DateTimeOffset.UtcNow,
        };

    // ── Behaviour ─────────────────────────────────────────────────────────────

    public void Activate() => Status = StoreStatus.Active;
    public void Suspend() => Status = StoreStatus.Suspended;
    public void SetFeatured(bool value) => IsFeatured = value;
    public void SetLogoUrl(string url) => LogoUrl = url;

    public void UpdateProfile(
        string name, string description, string address,
        decimal lat, decimal lng, string? phoneNumber, string? website)
    {
        Name = name.Trim();
        Description = description.Trim();
        Address = address.Trim();
        Lat = lat;
        Lng = lng;
        if (phoneNumber is not null) PhoneNumber = phoneNumber.Trim();
        if (website is not null) Website = website.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

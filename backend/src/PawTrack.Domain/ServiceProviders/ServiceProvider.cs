namespace PawTrack.Domain.ServiceProviders;

public sealed class ServiceProvider
{
    private ServiceProvider() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public ServiceProviderCategory Category { get; private set; }
    public string Address { get; private set; } = string.Empty;
    public decimal Lat { get; private set; }
    public decimal Lng { get; private set; }
    public string ContactEmail { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public string? Website { get; private set; }
    public string? LogoUrl { get; private set; }
    public bool IsFeatured { get; private set; }
    public ServiceProviderStatus Status { get; private set; }
    public string? SuspensionReason { get; private set; }
    public DateTimeOffset RegisteredAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public static ServiceProvider Create(
        Guid userId,
        string name,
        string description,
        ServiceProviderCategory category,
        string address,
        decimal lat,
        decimal lng,
        string contactEmail) => new()
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Name = name.Trim(),
            Description = description.Trim(),
            Category = category,
            Address = address.Trim(),
            Lat = lat,
            Lng = lng,
            ContactEmail = contactEmail.Trim().ToLowerInvariant(),
            Status = ServiceProviderStatus.Pending,
            RegisteredAt = DateTimeOffset.UtcNow,
        };

    public void Activate()
    {
        Status = ServiceProviderStatus.Active;
        SuspensionReason = null;
    }
    public void Reject() => Status = ServiceProviderStatus.Rejected;
    public void Suspend(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Se requiere un motivo de suspension.");
        Status = ServiceProviderStatus.Suspended;
        SuspensionReason = reason.Trim();
    }
    public void SetFeatured(bool value) => IsFeatured = value;
    public void SetLogoUrl(string url) => LogoUrl = url;

    public void UpdateProfile(
        string name,
        string description,
        ServiceProviderCategory category,
        string address,
        decimal lat,
        decimal lng,
        string? phoneNumber,
        string? website)
    {
        Name = name.Trim();
        Description = description.Trim();
        Category = category;
        Address = address.Trim();
        Lat = lat;
        Lng = lng;
        PhoneNumber = phoneNumber is null ? PhoneNumber : (phoneNumber.Trim() is { Length: > 0 } phone ? phone : null);
        Website = website is null ? Website : (website.Trim() is { Length: > 0 } site ? site : null);
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
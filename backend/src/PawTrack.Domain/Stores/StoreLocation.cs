namespace PawTrack.Domain.Stores;

/// <summary>
/// A physical branch/sede belonging to a Store. Multi-location is a StorePartner-tier
/// feature — gating is enforced in the application layer, not here.
/// </summary>
public sealed class StoreLocation
{
    private StoreLocation() { } // EF Core

    public Guid Id { get; private set; }
    public Guid StoreId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public decimal Lat { get; private set; }
    public decimal Lng { get; private set; }
    public string? PhoneNumber { get; private set; }
    /// <summary>True for the store's original/default location — cannot be deactivated.</summary>
    public bool IsPrimary { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public static StoreLocation Create(
        Guid storeId, string name, string address, decimal lat, decimal lng,
        string? phoneNumber, bool isPrimary = false) => new()
        {
            Id = Guid.CreateVersion7(),
            StoreId = storeId,
            Name = name.Trim(),
            Address = address.Trim(),
            Lat = lat,
            Lng = lng,
            PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim(),
            IsPrimary = isPrimary,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

    public void UpdateDetails(string name, string address, decimal lat, decimal lng, string? phoneNumber)
    {
        Name = name.Trim();
        Address = address.Trim();
        Lat = lat;
        Lng = lng;
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        if (IsPrimary)
            throw new InvalidOperationException("La sede principal no se puede desactivar.");
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Reactivate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

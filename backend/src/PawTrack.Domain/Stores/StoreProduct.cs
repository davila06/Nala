namespace PawTrack.Domain.Stores;

public sealed class StoreProduct
{
    private StoreProduct() { }

    public Guid Id { get; private set; }
    public Guid StoreId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ProductCategory Category { get; private set; }
    /// <summary>Price in CRC colones.</summary>
    public decimal PriceCrc { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsAvailable { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static StoreProduct Create(
        Guid storeId,
        string name,
        string? description,
        ProductCategory category,
        decimal priceCrc) => new()
        {
            Id = Guid.CreateVersion7(),
            StoreId = storeId,
            Name = name.Trim(),
            Description = description?.Trim(),
            Category = category,
            PriceCrc = priceCrc,
            IsAvailable = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

    public void Update(string name, string? description, ProductCategory category, decimal priceCrc)
    {
        Name = name.Trim();
        Description = description?.Trim();
        Category = category;
        PriceCrc = priceCrc;
    }

    public void SetImageUrl(string url) => ImageUrl = url;
    public void SetAvailable(bool available) => IsAvailable = available;
}

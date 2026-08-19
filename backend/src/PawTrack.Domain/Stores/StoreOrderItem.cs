namespace PawTrack.Domain.Stores;

public sealed class StoreOrderItem
{
    private StoreOrderItem() { }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    /// <summary>Snapshot of the product name at order time — survives product edits.</summary>
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    /// <summary>Unit price at order time in CRC.</summary>
    public decimal UnitPriceCrc { get; private set; }

    public decimal SubtotalCrc => UnitPriceCrc * Quantity;

    internal static StoreOrderItem Create(
        Guid orderId, Guid productId, string productName, int quantity, decimal unitPriceCrc) => new()
        {
            Id = Guid.CreateVersion7(),
            OrderId = orderId,
            ProductId = productId,
            ProductName = productName,
            Quantity = quantity,
            UnitPriceCrc = unitPriceCrc,
        };
}

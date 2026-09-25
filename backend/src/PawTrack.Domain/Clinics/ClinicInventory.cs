namespace PawTrack.Domain.Clinics;

public enum ClinicInventoryItemType
{
    Vaccine,
    Medication,
    Dewormer,
    Supply,
    Food,
    Service,
}

public enum ClinicInventoryMovementReason
{
    StockReceived,
    ConsultationUse,
    Sale,
    Adjustment,
    Expired,
    Damaged,
}

public sealed class ClinicInventoryItem
{
    private ClinicInventoryItem() { }

    public Guid Id { get; private set; }
    public Guid ClinicId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public ClinicInventoryItemType Type { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public int MinimumStock { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static ClinicInventoryItem Create(
        Guid clinicId,
        string name,
        ClinicInventoryItemType type,
        string unit,
        int minimumStock)
    {
        if (clinicId == Guid.Empty) throw new ArgumentException("ClinicId is required.", nameof(clinicId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(unit)) throw new ArgumentException("Unit is required.", nameof(unit));
        if (minimumStock < 0) throw new ArgumentOutOfRangeException(nameof(minimumStock));

        return new ClinicInventoryItem
        {
            Id = Guid.CreateVersion7(),
            ClinicId = clinicId,
            Name = name.Trim(),
            Type = type,
            Unit = unit.Trim(),
            MinimumStock = minimumStock,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Update(string name, string unit, int minimumStock, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(unit)) throw new ArgumentException("Unit is required.", nameof(unit));
        if (minimumStock < 0) throw new ArgumentOutOfRangeException(nameof(minimumStock));

        Name = name.Trim();
        Unit = unit.Trim();
        MinimumStock = minimumStock;
        IsActive = isActive;
    }

    public bool IsBelowMinimum(int totalAvailable) => totalAvailable < MinimumStock;
}

public sealed class ClinicInventoryLot
{
    private ClinicInventoryLot() { }

    public Guid Id { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid ItemId { get; private set; }
    public string LotNumber { get; private set; } = string.Empty;
    public DateOnly? ExpiresAt { get; private set; }
    public int InitialQuantity { get; private set; }
    public int AvailableQuantity { get; private set; }
    public decimal UnitCostCrc { get; private set; }
    public string? SupplierName { get; private set; }
    public string LocationName { get; private set; } = "Principal";
    public DateTimeOffset ReceivedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static ClinicInventoryLot Receive(
        Guid clinicId,
        Guid itemId,
        string lotNumber,
        DateOnly? expiresAt,
        int quantity,
        decimal unitCostCrc,
        string? supplierName,
        string? locationName = null)
    {
        if (clinicId == Guid.Empty) throw new ArgumentException("ClinicId is required.", nameof(clinicId));
        if (itemId == Guid.Empty) throw new ArgumentException("ItemId is required.", nameof(itemId));
        if (string.IsNullOrWhiteSpace(lotNumber)) throw new ArgumentException("Lot number is required.", nameof(lotNumber));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (unitCostCrc < 0) throw new ArgumentOutOfRangeException(nameof(unitCostCrc));

        return new ClinicInventoryLot
        {
            Id = Guid.CreateVersion7(),
            ClinicId = clinicId,
            ItemId = itemId,
            LotNumber = lotNumber.Trim().ToUpperInvariant(),
            ExpiresAt = expiresAt,
            InitialQuantity = quantity,
            AvailableQuantity = quantity,
            UnitCostCrc = unitCostCrc,
            SupplierName = string.IsNullOrWhiteSpace(supplierName) ? null : supplierName.Trim(),
            LocationName = string.IsNullOrWhiteSpace(locationName) ? "Principal" : locationName.Trim(),
            ReceivedAt = DateTimeOffset.UtcNow,
        };
    }

    public ClinicInventoryMovement Consume(
        int quantity,
        ClinicInventoryMovementReason reason,
        Guid actorUserId,
        Guid? petId,
        Guid? consultationId,
        Guid? certificateId)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (quantity > AvailableQuantity) throw new InvalidOperationException("No hay stock suficiente en el lote.");

        AvailableQuantity -= quantity;
        return ClinicInventoryMovement.Create(
            ClinicId,
            ItemId,
            Id,
            -quantity,
            reason,
            actorUserId,
            petId,
            consultationId,
            certificateId);
    }

    public ClinicInventoryMovement Adjust(
        int quantityDelta,
        Guid actorUserId,
        string reason)
    {
        if (quantityDelta == 0) throw new ArgumentOutOfRangeException(nameof(quantityDelta));
        if (AvailableQuantity + quantityDelta < 0) throw new InvalidOperationException("El ajuste dejaría stock negativo.");

        AvailableQuantity += quantityDelta;
        return ClinicInventoryMovement.Create(
            ClinicId,
            ItemId,
            Id,
            quantityDelta,
            ClinicInventoryMovementReason.Adjustment,
            actorUserId,
            null,
            null,
            null,
            reason);
    }
}

public sealed class ClinicInventoryMovement
{
    private ClinicInventoryMovement() { }

    public Guid Id { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid ItemId { get; private set; }
    public Guid LotId { get; private set; }
    public int QuantityDelta { get; private set; }
    public ClinicInventoryMovementReason Reason { get; private set; }
    public Guid ActorUserId { get; private set; }
    public Guid? PetId { get; private set; }
    public Guid? ConsultationId { get; private set; }
    public Guid? CertificateId { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static ClinicInventoryMovement Create(
        Guid clinicId,
        Guid itemId,
        Guid lotId,
        int quantityDelta,
        ClinicInventoryMovementReason reason,
        Guid actorUserId,
        Guid? petId,
        Guid? consultationId,
        Guid? certificateId,
        string? notes = null)
    {
        if (clinicId == Guid.Empty) throw new ArgumentException("ClinicId is required.", nameof(clinicId));
        if (itemId == Guid.Empty) throw new ArgumentException("ItemId is required.", nameof(itemId));
        if (lotId == Guid.Empty) throw new ArgumentException("LotId is required.", nameof(lotId));
        if (quantityDelta == 0) throw new ArgumentOutOfRangeException(nameof(quantityDelta));
        if (actorUserId == Guid.Empty) throw new ArgumentException("ActorUserId is required.", nameof(actorUserId));

        return new ClinicInventoryMovement
        {
            Id = Guid.CreateVersion7(),
            ClinicId = clinicId,
            ItemId = itemId,
            LotId = lotId,
            QuantityDelta = quantityDelta,
            Reason = reason,
            ActorUserId = actorUserId,
            PetId = petId,
            ConsultationId = consultationId,
            CertificateId = certificateId,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}

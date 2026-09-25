using FluentAssertions;
using PawTrack.Domain.Clinics;

namespace PawTrack.UnitTests.Clinics;

public sealed class ClinicInventoryDomainTests
{
    [Fact]
    public void LotConsume_ReducesAvailableQuantityAndRecordsTrace()
    {
        var item = ClinicInventoryItem.Create(
            Guid.NewGuid(),
            "Vacuna rabia",
            ClinicInventoryItemType.Vaccine,
            "unidad",
            minimumStock: 3);
        var lot = ClinicInventoryLot.Receive(
            item.ClinicId,
            item.Id,
            "RAB-001",
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)),
            quantity: 10,
            unitCostCrc: 1200m,
            supplierName: "Proveedor CR");

        var movement = lot.Consume(
            quantity: 2,
            reason: ClinicInventoryMovementReason.ConsultationUse,
            actorUserId: Guid.NewGuid(),
            petId: Guid.NewGuid(),
            consultationId: Guid.NewGuid(),
            certificateId: null);

        lot.AvailableQuantity.Should().Be(8);
        movement.QuantityDelta.Should().Be(-2);
        movement.LotId.Should().Be(lot.Id);
        movement.PetId.Should().NotBeNull();
        item.IsBelowMinimum(totalAvailable: lot.AvailableQuantity).Should().BeFalse();
    }

    [Fact]
    public void LotConsume_RejectsQuantityAboveAvailable()
    {
        var lot = ClinicInventoryLot.Receive(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "MED-001",
            DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(6)),
            quantity: 1,
            unitCostCrc: 500m,
            supplierName: null);

        var act = () => lot.Consume(
            2,
            ClinicInventoryMovementReason.ConsultationUse,
            Guid.NewGuid(),
            null,
            null,
            null);

        act.Should().Throw<InvalidOperationException>();
    }
}

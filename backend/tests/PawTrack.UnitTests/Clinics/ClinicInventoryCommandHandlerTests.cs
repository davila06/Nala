using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Clinics.Commands.ManageClinicInventory;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Clinics;

namespace PawTrack.UnitTests.Clinics;

public sealed class ClinicInventoryCommandHandlerTests
{
    [Fact]
    public async Task ConsumeInventory_UsesOldestAvailableLotFirstAndPersistsMovement()
    {
        var clinicUserId = Guid.NewGuid();
        var clinic = Clinic.Create(clinicUserId, "Clinica Test", "VET-123", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        clinic.Activate();
        var item = ClinicInventoryItem.Create(clinic.Id, "Vacuna rabia", ClinicInventoryItemType.Vaccine, "unidad", 2);
        var older = ClinicInventoryLot.Receive(clinic.Id, item.Id, "OLD", DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)), 1, 1000m, null);
        var newer = ClinicInventoryLot.Receive(clinic.Id, item.Id, "NEW", DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(8)), 5, 1100m, null);

        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var inventory = Substitute.For<IClinicInventoryRepository>();
        inventory.GetItemByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        inventory.GetAvailableLotsByItemAsync(item.Id, Arg.Any<CancellationToken>()).Returns([older, newer]);
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new ConsumeClinicInventoryCommandHandler(clinics, inventory, unitOfWork);

        var result = await handler.Handle(new ConsumeClinicInventoryCommand(
            clinic.Id,
            clinicUserId,
            item.Id,
            2,
            ClinicInventoryMovementReason.ConsultationUse,
            PetId: Guid.NewGuid(),
            ConsultationId: Guid.NewGuid(),
            CertificateId: null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        older.AvailableQuantity.Should().Be(0);
        newer.AvailableQuantity.Should().Be(4);
        await inventory.Received(2).AddMovementAsync(Arg.Any<ClinicInventoryMovement>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConsumeInventory_WhenRequestedQuantityExceedsAvailable_DoesNotPersistNegativeStock()
    {
        var clinicUserId = Guid.NewGuid();
        var clinic = Clinic.Create(clinicUserId, "Clinica Test", "VET-123", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        clinic.Activate();
        var item = ClinicInventoryItem.Create(clinic.Id, "Medicamento", ClinicInventoryItemType.Medication, "unidad", 1);
        var lot = ClinicInventoryLot.Receive(clinic.Id, item.Id, "MED-1", DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2)), 1, 500m, null);

        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var inventory = Substitute.For<IClinicInventoryRepository>();
        inventory.GetItemByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        inventory.GetAvailableLotsByItemAsync(item.Id, Arg.Any<CancellationToken>()).Returns([lot]);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new ConsumeClinicInventoryCommandHandler(clinics, inventory, unitOfWork);

        var result = await handler.Handle(new ConsumeClinicInventoryCommand(
            clinic.Id,
            clinicUserId,
            item.Id,
            2,
            ClinicInventoryMovementReason.ConsultationUse,
            PetId: Guid.NewGuid(),
            ConsultationId: Guid.NewGuid(),
            CertificateId: null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        lot.AvailableQuantity.Should().Be(1);
        inventory.DidNotReceive().UpdateLot(Arg.Any<ClinicInventoryLot>());
        await inventory.DidNotReceive().AddMovementAsync(Arg.Any<ClinicInventoryMovement>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Clinics.Commands.CloseClinicalConsultation;
using PawTrack.Application.Clinics.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Clinics;
using PawTrack.Domain.Medical;

namespace PawTrack.UnitTests.Clinics;

public sealed class CloseClinicalConsultationInventoryTests
{
    [Fact]
    public async Task Handle_WithInventoryUses_ConsumesStockWithConsultationTrace()
    {
        var clinicUserId = Guid.NewGuid();
        var clinicId = Guid.NewGuid();
        var item = ClinicInventoryItem.Create(clinicId, "Vacuna rabia", ClinicInventoryItemType.Vaccine, "unidad", 1);
        var lot = ClinicInventoryLot.Receive(clinicId, item.Id, "RAB-001", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)), 3, 1200m, null);
        var appointment = VeterinarianAppointment.Schedule(clinicId, Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(-1), TimeSpan.FromMinutes(30), clinicUserId);
        appointment.Confirm();
        appointment.CheckIn();
        appointment.StartConsultation();
        var consultation = ClinicalConsultation.Create(
            clinicId, appointment.Id, appointment.PetId, appointment.VeterinarianId, Guid.NewGuid(), clinicUserId,
            "Vacuna", "S", "O", "A", "P", null, null, null, null, null, null, null,
            "Dx", "Tx", "Resumen");

        var consultations = Substitute.For<IClinicalConsultationRepository>();
        consultations.GetByIdAsync(consultation.Id, Arg.Any<CancellationToken>()).Returns(consultation);
        var appointments = Substitute.For<IVeterinarianAppointmentRepository>();
        appointments.GetByIdAsync(appointment.Id, Arg.Any<CancellationToken>()).Returns(appointment);
        var inventory = Substitute.For<IClinicInventoryRepository>();
        inventory.GetItemByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        inventory.GetAvailableLotsByItemAsync(item.Id, Arg.Any<CancellationToken>()).Returns([lot]);
        var medical = Substitute.For<IMedicalRepository>();
        var audit = Substitute.For<IAuditLogRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new CloseClinicalConsultationCommandHandler(
            consultations,
            appointments,
            medical,
            inventory,
            audit,
            unitOfWork);

        var result = await handler.Handle(new CloseClinicalConsultationCommand(
            clinicId,
            clinicUserId,
            consultation.Id,
            "Dra. Ana",
            [new ClinicalInventoryUseInput(item.Id, 1, ClinicInventoryMovementReason.ConsultationUse)]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        lot.AvailableQuantity.Should().Be(2);
        await inventory.Received(1).AddMovementAsync(
            Arg.Is<ClinicInventoryMovement>(movement => movement.ConsultationId == consultation.Id && movement.PetId == consultation.PetId),
            Arg.Any<CancellationToken>());
    }
}

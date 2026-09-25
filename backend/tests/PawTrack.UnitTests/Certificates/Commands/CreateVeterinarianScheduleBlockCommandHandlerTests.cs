using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Certificates.Commands.CreateVeterinarianScheduleBlock;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Clinics;

namespace PawTrack.UnitTests.Certificates.Commands;

public sealed class CreateVeterinarianScheduleBlockCommandHandlerTests
{
    [Fact]
    public async Task Handle_AuthorizedVeterinarian_AddsBlockAndPersists()
    {
        var clinicUserId = Guid.NewGuid();
        var clinic = Clinic.Create(clinicUserId, "Clinica Test", "VET-123", "San Jose", 9.93m, -84.08m, "clinic@test.cr");
        clinic.Activate();
        var veterinarian = ClinicVeterinarian.Create(clinic.Id, "Dra. Ana", "VET-999");

        var clinics = Substitute.For<IClinicRepository>();
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);
        var veterinarians = Substitute.For<IClinicVeterinarianRepository>();
        veterinarians.GetByIdAsync(veterinarian.Id, Arg.Any<CancellationToken>()).Returns(veterinarian);
        var blocks = Substitute.For<IVeterinarianScheduleBlockRepository>();
        var appointments = Substitute.For<IVeterinarianAppointmentRepository>();
        var auditLog = Substitute.For<IAuditLogRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new CreateVeterinarianScheduleBlockCommandHandler(
            clinics,
            veterinarians,
            blocks,
            appointments,
            auditLog,
            unitOfWork);

        var result = await handler.Handle(
            new CreateVeterinarianScheduleBlockCommand(
                clinic.Id,
                clinicUserId,
                veterinarian.Id,
                DateTimeOffset.UtcNow.AddHours(2),
                DateTimeOffset.UtcNow.AddHours(3),
                "Cirugia"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await blocks.Received(1).AddAsync(Arg.Any<VeterinarianScheduleBlock>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

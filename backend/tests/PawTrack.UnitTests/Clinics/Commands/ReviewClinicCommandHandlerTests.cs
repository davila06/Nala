using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Clinics.Commands.ReviewClinic;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Clinics;

namespace PawTrack.UnitTests.Clinics.Commands;

public sealed class ReviewClinicCommandHandlerTests
{
    private readonly IClinicRepository _clinicRepo = Substitute.For<IClinicRepository>();
    private readonly IAuditLogRepository _auditLog = Substitute.For<IAuditLogRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private ReviewClinicCommandHandler CreateSut() =>
        new(_clinicRepo, _auditLog, _emailSender, _unitOfWork);

    [Fact]
    public async Task Handle_WhenClinicNotFound_ReturnsFailure()
    {
        _clinicRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Clinic?)null);

        var sut = CreateSut();
        var result = await sut.Handle(new ReviewClinicCommand(Guid.NewGuid(), true), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("Clínica no encontrada.");
        await _emailSender.DidNotReceive().SendClinicApprovedWelcomeAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenApproved_ActivatesClinic_AndSendsWelcomeEmail()
    {
        var clinic = Clinic.Create(
            Guid.NewGuid(),
            "Clínica San Martín",
            "SENASA-12345",
            "San José centro",
            9.93m,
            -84.08m,
            "contacto@clinicasanmartin.cr");

        _clinicRepo.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>())
            .Returns(clinic);

        var sut = CreateSut();
        var result = await sut.Handle(new ReviewClinicCommand(clinic.Id, true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        clinic.Status.Should().Be(ClinicStatus.Active);
        _clinicRepo.Received(1).Update(clinic);
        await _auditLog.Received(1).AddAsync(Arg.Is<AuditLogEntry>(a => a.Action == AuditAction.ClinicApproved), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        await _emailSender.Received(1).SendClinicApprovedWelcomeAsync(
            "contacto@clinicasanmartin.cr",
            "Clínica San Martín",
            "https://pawtrack.cr/login",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRejected_SuspendsClinic_DoesNotSendEmail()
    {
        var clinic = Clinic.Create(
            Guid.NewGuid(),
            "Clínica San Martín",
            "SENASA-12345",
            "San José centro",
            9.93m,
            -84.08m,
            "contacto@clinicasanmartin.cr");

        _clinicRepo.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>())
            .Returns(clinic);

        var sut = CreateSut();
        var result = await sut.Handle(new ReviewClinicCommand(clinic.Id, false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        clinic.Status.Should().Be(ClinicStatus.Suspended);
        _clinicRepo.Received(1).Update(clinic);
        await _auditLog.Received(1).AddAsync(Arg.Is<AuditLogEntry>(a => a.Action == AuditAction.ClinicRejected), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        await _emailSender.DidNotReceive().SendClinicApprovedWelcomeAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}

using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Application.Clinics.Commands.UpdateClinicProfile;
using PawTrack.Domain.Audit;
using PawTrack.Domain.Clinics;

namespace PawTrack.UnitTests.Clinics.Commands;

public sealed class UpdateClinicProfileCommandHandlerTests
{
    [Fact]
    public async Task Handle_OwnerUpdatesProfile_AndWritesAuditEntry()
    {
        var ownerId = Guid.NewGuid();
        var clinic = Clinic.Create(
            ownerId, "Vet Salud", "SENASA-123", "San Jose", 9.93m, -84.08m, "vet@example.com");
        var clinics = Substitute.For<IClinicRepository>();
        var audits = Substitute.For<IAuditLogRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        clinics.GetByUserIdAsync(ownerId, Arg.Any<CancellationToken>()).Returns(clinic);

        var handler = new UpdateClinicProfileCommandHandler(clinics, audits, unitOfWork);

        var result = await handler.Handle(new UpdateClinicProfileCommand(
            ownerId,
            "Vet Salud 24",
            "Heredia",
            "+506 2222 3333",
            "https://vetsalud.example",
            true,
            "+506 8888 9999"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Vet Salud 24");
        result.Value.Address.Should().Be("Heredia");
        result.Value.IsEmergency24h.Should().BeTrue();
        await audits.Received(1).AddAsync(
            Arg.Is<AuditLogEntry>(entry =>
                entry.Action == AuditAction.ClinicProfileUpdated
                && entry.EntityId == clinic.Id.ToString()
                && entry.AdminUserId == ownerId),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonOwnerCannotUpdateClinicProfile()
    {
        var ownerId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        var clinic = Clinic.Create(
            ownerId, "Vet Salud", "SENASA-123", "San Jose", 9.93m, -84.08m, "vet@example.com");
        var clinics = Substitute.For<IClinicRepository>();
        var audits = Substitute.For<IAuditLogRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        clinics.GetByUserIdAsync(strangerId, Arg.Any<CancellationToken>()).Returns((Clinic?)null);

        var handler = new UpdateClinicProfileCommandHandler(clinics, audits, unitOfWork);

        var result = await handler.Handle(new UpdateClinicProfileCommand(
            strangerId, "Intruso", "Otra dirección", null, null, null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await audits.DidNotReceive().AddAsync(Arg.Any<AuditLogEntry>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
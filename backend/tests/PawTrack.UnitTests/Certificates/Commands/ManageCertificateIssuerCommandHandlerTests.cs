using FluentAssertions;
using NSubstitute;
using PawTrack.Application.Certificates.Commands.ManageCertificateIssuers;
using PawTrack.Application.Certificates.Interfaces;
using PawTrack.Application.Common.Interfaces;
using PawTrack.Domain.Certificates;
using PawTrack.Domain.Clinics;

namespace PawTrack.UnitTests.Certificates.Commands;

public sealed class ManageCertificateIssuerCommandHandlerTests
{
    [Fact]
    public async Task SubmitClinicVerification_KnownClinic_CreatesPendingVerification()
    {
        var clinics = Substitute.For<IClinicRepository>();
        var verifications = Substitute.For<IClinicVerificationRepository>();
        var auditLogs = Substitute.For<IVerificationAuditLogRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clinic = Clinic.Create(Guid.NewGuid(), "VetSalud", "SENASA-12345", "Heredia", 10m, -84.1m, "vet@example.com");
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);

        var handler = new SubmitClinicVerificationCommandHandler(clinics, verifications, auditLogs, unitOfWork);

        var result = await handler.Handle(
            new SubmitClinicVerificationCommand(clinic.Id, clinic.UserId),
            default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ClinicVerificationStatus.Pending.ToString());
        await verifications.Received(1).AddAsync(Arg.Is<ClinicVerification>(v => v.ClinicId == clinic.Id && v.Status == ClinicVerificationStatus.Pending), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateClinicVeterinarian_ClinicOwner_CreatesPendingVeterinarian()
    {
        var clinics = Substitute.For<IClinicRepository>();
        var veterinarians = Substitute.For<IClinicVeterinarianRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var clinicOwnerId = Guid.NewGuid();
        var clinic = Clinic.Create(clinicOwnerId, "VetSalud", "SENASA-12345", "Heredia", 10m, -84.1m, "vet@example.com");
        clinics.GetByIdAsync(clinic.Id, Arg.Any<CancellationToken>()).Returns(clinic);

        var handler = new CreateClinicVeterinarianCommandHandler(clinics, veterinarians, unitOfWork);

        var result = await handler.Handle(
            new CreateClinicVeterinarianCommand(clinic.Id, clinicOwnerId, "Dra. Rivera", "vet-12345"),
            default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.LicenseNumber.Should().Be("VET-12345");
        result.Value.Status.Should().Be(ClinicVeterinarianStatus.PendingReview.ToString());
        await veterinarians.Received(1).AddAsync(Arg.Is<ClinicVeterinarian>(v => v.ClinicId == clinic.Id && v.Status == ClinicVeterinarianStatus.PendingReview), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

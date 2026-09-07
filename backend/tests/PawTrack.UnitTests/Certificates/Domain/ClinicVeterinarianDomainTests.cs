using FluentAssertions;
using PawTrack.Domain.Certificates;

namespace PawTrack.UnitTests.Certificates.Domain;

public sealed class ClinicVeterinarianDomainTests
{
    [Fact]
    public void Submit_NormalizesLicenseAndStartsPendingReview()
    {
        var clinicId = Guid.NewGuid();
        var submittedBy = Guid.NewGuid();

        var veterinarian = ClinicVeterinarian.Submit(clinicId, submittedBy, "Dra. Rivera", " vet-12345 ");

        veterinarian.ClinicId.Should().Be(clinicId);
        veterinarian.SubmittedByUserId.Should().Be(submittedBy);
        veterinarian.FullName.Should().Be("Dra. Rivera");
        veterinarian.LicenseNumber.Should().Be("VET-12345");
        veterinarian.Status.Should().Be(ClinicVeterinarianStatus.PendingReview);
        veterinarian.CanIssueCertificates.Should().BeFalse();
        veterinarian.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Authorize_WithDocumentAndExpiration_AllowsCertificateIssuance()
    {
        var veterinarian = ClinicVeterinarian.Submit(Guid.NewGuid(), Guid.NewGuid(), "Dra. Rivera", "VET-12345");
        veterinarian.AttachDocument("https://storage/vet.pdf");
        var reviewedBy = Guid.NewGuid();
        var expiresAt = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1));

        var result = veterinarian.Authorize(reviewedBy, expiresAt, "Licencia validada");

        result.IsSuccess.Should().BeTrue();
        veterinarian.Status.Should().Be(ClinicVeterinarianStatus.Authorized);
        veterinarian.CanIssueCertificates.Should().BeTrue();
        veterinarian.IsActive.Should().BeTrue();
        veterinarian.ReviewedByAdminUserId.Should().Be(reviewedBy);
        veterinarian.ReviewNotes.Should().Be("Licencia validada");
        veterinarian.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public void Revoke_WithReason_DisablesCertificateIssuance()
    {
        var veterinarian = ClinicVeterinarian.Submit(Guid.NewGuid(), Guid.NewGuid(), "Dra. Rivera", "VET-12345");
        veterinarian.AttachDocument("https://storage/vet.pdf");
        veterinarian.Authorize(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)), null);
        var revokedBy = Guid.NewGuid();

        var result = veterinarian.Revoke(revokedBy, "Ya no labora en la clinica");

        result.IsSuccess.Should().BeTrue();
        veterinarian.CanIssueCertificates.Should().BeFalse();
        veterinarian.IsActive.Should().BeFalse();
        veterinarian.Status.Should().Be(ClinicVeterinarianStatus.Revoked);
        veterinarian.RevokedByUserId.Should().Be(revokedBy);
        veterinarian.RevocationReason.Should().Be("Ya no labora en la clinica");
        veterinarian.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public void Authorize_WithoutDocument_ReturnsFailure()
    {
        var veterinarian = ClinicVeterinarian.Submit(Guid.NewGuid(), Guid.NewGuid(), "Dra. Rivera", "VET-12345");

        var result = veterinarian.Authorize(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)), null);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("El documento del veterinario es requerido.");
        veterinarian.Status.Should().Be(ClinicVeterinarianStatus.PendingReview);
    }
}

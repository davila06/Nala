using FluentAssertions;
using PawTrack.Domain.Certificates;

namespace PawTrack.UnitTests.Certificates.Domain;

public sealed class ClinicVerificationDomainTests
{
    [Fact]
    public void Verify_RecordsReviewerAndExpiration()
    {
        var verification = ClinicVerification.Submit(Guid.NewGuid(), " senasa-12345 ", Guid.NewGuid());
        verification.AttachDocument("https://storage/verification.pdf");
        var reviewedBy = Guid.NewGuid();
        var expiresAt = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1));

        var result = verification.Verify(reviewedBy, expiresAt, "Documento coincide con la licencia");

        result.IsSuccess.Should().BeTrue();
        verification.Status.Should().Be(ClinicVerificationStatus.Verified);
        verification.LicenseNumberSnapshot.Should().Be("SENASA-12345");
        verification.ReviewedByAdminUserId.Should().Be(reviewedBy);
        verification.ReviewedAt.Should().NotBeNull();
        verification.ReviewNotes.Should().Be("Documento coincide con la licencia");
        verification.ExpiresAt.Should().Be(expiresAt);
        verification.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Reject_WithReason_MarksAsRejected()
    {
        var verification = ClinicVerification.Submit(Guid.NewGuid(), "SENASA-12345", Guid.NewGuid());

        var result = verification.Reject(Guid.NewGuid(), "Documento ilegible");

        result.IsSuccess.Should().BeTrue();
        verification.Status.Should().Be(ClinicVerificationStatus.Rejected);
        verification.IsActive.Should().BeFalse();
        verification.RejectionReason.Should().Be("Documento ilegible");
    }

    [Fact]
    public void Verify_WithoutDocument_ReturnsFailure()
    {
        var verification = ClinicVerification.Submit(Guid.NewGuid(), "SENASA-12345", Guid.NewGuid());

        var result = verification.Verify(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)), null);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain("El documento de verificación es requerido.");
        verification.Status.Should().Be(ClinicVerificationStatus.Pending);
    }

    [Fact]
    public void MarkExpired_DisablesActiveVerification()
    {
        var verification = ClinicVerification.Submit(Guid.NewGuid(), "SENASA-12345", Guid.NewGuid());
        verification.AttachDocument("https://storage/verification.pdf");
        verification.Verify(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)), null);

        verification.MarkExpired();

        verification.Status.Should().Be(ClinicVerificationStatus.Expired);
        verification.IsActive.Should().BeFalse();
    }
}

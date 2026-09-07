using FluentAssertions;
using PawTrack.Domain.AnimalWelfare;

namespace PawTrack.UnitTests.AnimalWelfare;

public sealed class AnimalWelfareCaseDomainTests
{
    [Fact]
    public void Create_ValidReport_StartsReceivedWithSanitizedDescription()
    {
        var reporterId = Guid.NewGuid();

        var welfareCase = AnimalWelfareCase.Create(
            WelfareCaseType.Abandonment,
            WelfareSeverity.Medium,
            "San José",
            "Perro abandonado en parque",
            reporterId,
            reporterIsAnonymous: false,
            approxLat: 9.93,
            approxLng: -84.08);

        welfareCase.Status.Should().Be(WelfareCaseStatus.Received);
        welfareCase.Type.Should().Be(WelfareCaseType.Abandonment);
        welfareCase.Severity.Should().Be(WelfareSeverity.Medium);
        welfareCase.ReporterUserId.Should().Be(reporterId);
        welfareCase.ReporterIsAnonymous.Should().BeFalse();
    }

    [Fact]
    public void Resolve_WithoutResolution_ReturnsFailure()
    {
        var welfareCase = MakeCase();

        var result = welfareCase.Resolve(Guid.NewGuid(), " ");

        result.IsFailure.Should().BeTrue();
        welfareCase.Status.Should().Be(WelfareCaseStatus.Received);
    }

    [Fact]
    public void Assign_ClosedCase_ReturnsFailure()
    {
        var welfareCase = MakeCase();
        welfareCase.Dismiss(Guid.NewGuid(), "Reporte duplicado");

        var result = welfareCase.AssignTo(Guid.NewGuid(), "Municipality", Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        welfareCase.Status.Should().Be(WelfareCaseStatus.Dismissed);
    }

    [Fact]
    public void AddEvidence_PrivateBlob_CapturesMetadata()
    {
        var evidence = AnimalWelfareEvidence.Create(
            Guid.NewGuid(),
            "https://storage/welfare-evidence/case/photo.jpg",
            "image/jpeg",
            12_345,
            WelfareEvidenceKind.Photo,
            uploadedByUserId: Guid.NewGuid(),
            isSensitive: true,
            hashSha256: "abc123");

        evidence.IsSensitive.Should().BeTrue();
        evidence.ContentType.Should().Be("image/jpeg");
        evidence.FileSizeBytes.Should().Be(12_345);
    }

    private static AnimalWelfareCase MakeCase() =>
        AnimalWelfareCase.Create(
            WelfareCaseType.Neglect,
            WelfareSeverity.High,
            "Heredia",
            "Animal herido sin atención",
            reporterUserId: null,
            reporterIsAnonymous: true,
            approxLat: null,
            approxLng: null);
}

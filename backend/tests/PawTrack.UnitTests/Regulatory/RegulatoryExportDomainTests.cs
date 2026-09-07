using FluentAssertions;
using PawTrack.Domain.Common;
using PawTrack.Domain.Regulatory;

namespace PawTrack.UnitTests.Regulatory;

public sealed class RegulatoryExportDomainTests
{
    [Fact]
    public void Request_ValidParameters_StartsAsRequested()
    {
        var export = RegulatoryExport.Request(
            ReportType.WelfareCases,
            ExportScope.Institutional,
            ExportFormat.Json,
            schemaVersion: "1.0",
            requestedByUserId: Guid.NewGuid(),
            organizationId: Guid.NewGuid(),
            canton: "San José",
            periodStart: new DateOnly(2026, 1, 1),
            periodEnd: new DateOnly(2026, 1, 31),
            timeZone: "America/Costa_Rica",
            idempotencyKey: "request-123");

        export.Status.Should().Be(RegulatoryExportStatus.Requested);
        export.ExportCode.Should().NotBeNullOrWhiteSpace();
        export.SchemaVersion.Should().Be("1.0");
        export.IsDownloadable.Should().BeFalse();
    }

    [Fact]
    public void Complete_ThenCompleteAgain_ReturnsFailure()
    {
        var export = CreateExport();

        export.Start(DateTimeOffset.UtcNow).IsSuccess.Should().BeTrue();
        export.Complete(
            rowCount: 12,
            suppressedRowCount: 2,
            payloadSha256: new string('a', 64),
            blobUrl: "https://storage/regulatory-exports/export.json",
            completedAt: DateTimeOffset.UtcNow,
            expiresAt: DateTimeOffset.UtcNow.AddDays(30)).IsSuccess.Should().BeTrue();

        var secondCompletion = export.Complete(
            rowCount: 13,
            suppressedRowCount: 2,
            payloadSha256: new string('b', 64),
            blobUrl: "https://storage/regulatory-exports/other.json",
            completedAt: DateTimeOffset.UtcNow,
            expiresAt: DateTimeOffset.UtcNow.AddDays(30));

        secondCompletion.IsFailure.Should().BeTrue();
        export.RowCount.Should().Be(12);
        export.IsDownloadable.Should().BeTrue();
    }

    [Fact]
    public void Expire_CompletedExport_MakesItUndownloadable()
    {
        var export = CreateExport();
        export.Start(DateTimeOffset.UtcNow);
        export.Complete(1, 0, new string('a', 64), "https://storage/export.json", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1));

        var result = export.Expire(DateTimeOffset.UtcNow.AddDays(2));

        result.IsSuccess.Should().BeTrue();
        export.Status.Should().Be(RegulatoryExportStatus.Expired);
        export.IsDownloadable.Should().BeFalse();
    }

    [Fact]
    public void Complete_InvalidHashOrBlob_ReturnsFailure()
    {
        var export = CreateExport();
        export.Start(DateTimeOffset.UtcNow);

        var result = export.Complete(1, 0, "bad", " ", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1));

        result.IsFailure.Should().BeTrue();
        export.Status.Should().Be(RegulatoryExportStatus.Running);
    }

    [Fact]
    public void RegulatorySubmission_PreparedWithExport_CannotBeQueuedTwice()
    {
        var export = CreateExport();
        var submission = RegulatorySubmission.Prepare(
            export.Id,
            "ManualExport",
            "InstitutionalPackage",
            "submission-123",
            new string('b', 64));

        submission.Queue().IsSuccess.Should().BeTrue();
        submission.Queue().IsFailure.Should().BeTrue();
        submission.Status.Should().Be(RegulatorySubmissionStatus.Queued);
    }

    [Fact]
    public void ReportDefinition_RejectsInvalidSuppressionThreshold()
    {
        var action = () => ReportDefinition.Create(
            ReportType.WelfareCases,
            "WELFARE_CASES",
            "Casos de bienestar",
            "1.0",
            ExportScope.Public,
            suppressionThreshold: 0,
            retentionDays: 30);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static RegulatoryExport CreateExport() => RegulatoryExport.Request(
        ReportType.WelfareCases,
        ExportScope.Institutional,
        ExportFormat.Json,
        "1.0",
        Guid.NewGuid(),
        Guid.NewGuid(),
        "Heredia",
        new DateOnly(2026, 1, 1),
        new DateOnly(2026, 1, 31),
        "America/Costa_Rica",
        Guid.NewGuid().ToString("N"));
}

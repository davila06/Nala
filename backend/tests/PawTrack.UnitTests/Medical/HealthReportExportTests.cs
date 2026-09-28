using FluentAssertions;
using PawTrack.Domain.Medical;

namespace PawTrack.UnitTests.Medical;

public sealed class HealthReportExportTests
{
    [Fact]
    public void Export_TransitionsFromQueuedToCompletedAndExpires()
    {
        var export = HealthReportExport.Queue(Guid.NewGuid(), Guid.NewGuid(), TimeSpan.FromHours(24));
        var now = DateTimeOffset.UtcNow;

        export.Status.Should().Be(HealthReportExportStatus.Queued);
        export.Start(now).Should().BeTrue();
        export.Status.Should().Be(HealthReportExportStatus.Processing);
        export.Complete("https://storage.invalid/medical-health-exports/report.pdf", 6000, now.AddMinutes(2)).Should().BeTrue();
        export.IsDownloadable.Should().BeTrue();
        export.ItemCount.Should().Be(6000);
        export.Expire(export.ExpiresAt).Should().BeTrue();
        export.IsDownloadable.Should().BeFalse();
    }

    [Fact]
    public void Export_CannotCompleteBeforeProcessing()
    {
        var export = HealthReportExport.Queue(Guid.NewGuid(), Guid.NewGuid(), TimeSpan.FromHours(24));

        export.Complete("https://storage.invalid/report.pdf", 3, DateTimeOffset.UtcNow).Should().BeFalse();
        export.Status.Should().Be(HealthReportExportStatus.Queued);
    }

    [Fact]
    public void Export_InterruptedProcessingRetriesThenFailsClosed()
    {
        var export = HealthReportExport.Queue(Guid.NewGuid(), Guid.NewGuid(), TimeSpan.FromHours(24));
        var now = DateTimeOffset.UtcNow;

        export.Start(now.AddHours(-4)).Should().BeTrue();
        export.RecoverInterrupted(now.AddHours(-2)).Should().BeTrue();
        export.Status.Should().Be(HealthReportExportStatus.Queued);
        export.Start(now.AddHours(-3)).Should().BeTrue();
        export.RecoverInterrupted(now.AddHours(-2)).Should().BeTrue();
        export.Status.Should().Be(HealthReportExportStatus.Queued);
        export.Start(now.AddHours(-3)).Should().BeTrue();
        export.RecoverInterrupted(now.AddHours(-2)).Should().BeTrue();
        export.Status.Should().Be(HealthReportExportStatus.Failed);
        export.ErrorCode.Should().Be("worker_interrupted");
    }
}

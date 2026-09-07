using FluentAssertions;
using PawTrack.Application.Regulatory.Dtos;
using PawTrack.Domain.Regulatory;
using PawTrack.Infrastructure.Regulatory;

namespace PawTrack.UnitTests.Regulatory;

public sealed class RegulatoryReportDataTests
{
    [Fact]
    public void RenderCsv_SupportsNonOverviewReportData()
    {
        var data = new RegulatoryReportData(
            ReportType.MunicipalCaptures,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            [new RegulatoryReportRow("Received", "San José", 7, false)],
            IsSuppressed: false,
            Overview: null);

        var result = new QuestPdfRegulatoryExportRenderer().Render(ExportFormat.Csv, data);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ContentType.Should().Contain("text/csv");
        System.Text.Encoding.UTF8.GetString(result.Value.Bytes).Should().Contain("Received");
    }
}

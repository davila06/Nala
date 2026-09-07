using FluentAssertions;
using PawTrack.Application.Regulatory.Renderers;

namespace PawTrack.UnitTests.Regulatory;

public sealed class RegulatoryRendererTests
{
    [Fact]
    public void RenderCsv_EscapesSeparatorsQuotesAndFormulaPrefixes()
    {
        var rows = new[]
        {
            new ReportRow("Canton", "Nombre", "Valor"),
            new ReportRow("San José", "=HYPERLINK(\"https://evil\")", "1,2"),
        };

        var csv = CsvReportRenderer.Render(rows);

        csv.Should().Contain("\"'=HYPERLINK(\"\"https://evil\"\")\"");
        csv.Should().Contain("\"1,2\"");
    }

    [Fact]
    public void RenderCsv_UsesUtf8BomForInstitutionalSpreadsheetCompatibility()
    {
        var bytes = CsvReportRenderer.RenderUtf8([new ReportRow("A", "B", "C")]);

        bytes.Take(3).Should().Equal(0xEF, 0xBB, 0xBF);
    }
}

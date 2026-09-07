using FluentAssertions;
using PawTrack.Application.Regulatory.Dtos;
using PawTrack.Domain.Regulatory;
using PawTrack.Infrastructure.Regulatory;

namespace PawTrack.UnitTests.Regulatory;

public sealed class RegulatoryPdfRendererTests
{
    [Fact]
    public void RenderPdf_ProducesPortablePdfDocument()
    {
        var overview = new NalaOverviewDto(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            12,
            8,
            10,
            6,
            4,
            3,
            20,
            15,
            DateTimeOffset.UtcNow,
            false);

        var result = new QuestPdfRegulatoryExportRenderer().Render(ExportFormat.Pdf, overview);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ContentType.Should().Be("application/pdf");
        result.Value.FileExtension.Should().Be("pdf");
        result.Value.Bytes.Take(5).Should().Equal(0x25, 0x50, 0x44, 0x46, 0x2D);
    }
}

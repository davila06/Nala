using PawTrack.Application.Regulatory.Dtos;
using PawTrack.Domain.Common;
using PawTrack.Domain.Regulatory;

namespace PawTrack.Application.Regulatory.Interfaces;

public sealed record RegulatoryExportPayload(byte[] Bytes, string ContentType, string FileExtension);

public interface IRegulatoryExportRenderer
{
    Result<RegulatoryExportPayload> Render(ExportFormat format, RegulatoryReportData reportData);
}

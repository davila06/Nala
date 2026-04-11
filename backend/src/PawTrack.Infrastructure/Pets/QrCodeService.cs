using QRCoder;
using PawTrack.Application.Common.Interfaces;

namespace PawTrack.Infrastructure.Pets;

public sealed class QrCodeService : IQrCodeService
{
    public byte[] GeneratePng(string content)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(pixelsPerModule: 10);
    }
}

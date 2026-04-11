namespace PawTrack.Application.Common.Interfaces;

/// <summary>Generates QR code images without dependency on a specific library.</summary>
public interface IQrCodeService
{
    /// <summary>Returns PNG bytes for a QR code encoding the given content.</summary>
    byte[] GeneratePng(string content);
}

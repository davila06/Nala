namespace PawTrack.Application.Common.Interfaces;

/// <summary>
/// Composes a WhatsApp-ready avatar image containing a profile background and pet QR overlay.
/// </summary>
public interface IWhatsAppAvatarService
{
    Task<byte[]> BuildAvatarAsync(
        string? sourcePhotoUrl,
        string profileUrl,
        string petName,
        CancellationToken cancellationToken = default);
}

namespace PawTrack.Application.Common.Interfaces;

/// <summary>Generates a printable A6 pet identity card PDF in memory.</summary>
public interface IPetIdCardService
{
    byte[] Generate(PetIdCardData data);
}

public sealed record PetIdCardData(
    string PetName,
    string Species,
    string? Breed,
    string? PhotoUrl,
    string OwnerName,
    /// <summary>Full public profile URL encoded into the QR code.</summary>
    string PublicProfileUrl);

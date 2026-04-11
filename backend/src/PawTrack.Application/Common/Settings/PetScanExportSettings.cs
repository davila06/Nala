namespace PawTrack.Application.Common.Settings;

/// <summary>
/// Configuration for QR-scan history export PDF signing.
/// Bound from the "PetScanExport" section.
/// The signing key must come from Key Vault in production.
/// </summary>
public sealed class PetScanExportSettings
{
    /// <summary>HMAC-SHA256 key used to sign the scan-history export payload.</summary>
    public string? SigningKey { get; init; }
}
